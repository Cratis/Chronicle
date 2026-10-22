// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an <see cref="ISinkWrites"/> running every write inside a fenced batch.
/// </summary>
/// <param name="tokens">The <see cref="IFenceTokenSource"/> giving the owner's current <see cref="FenceToken"/>.</param>
/// <param name="executor">The <see cref="IFencedBatchExecutor"/> running the batches.</param>
/// <remarks>
/// A write outside <see cref="InBatch{T}"/> becomes a batch of its own, so no write reaches the collection unfenced. The batches
/// of one sink run one at a time: they all write the same gate record, so running them concurrently would only make them abort
/// each other with write conflicts.
/// </remarks>
public sealed class FencedSinkWrites(IFenceTokenSource tokens, IFencedBatchExecutor executor) : ISinkWrites, IDisposable
{
    readonly AsyncLocal<IClientSessionHandle?> _session = new();
    readonly AsyncLocal<List<Action>?> _afterCommit = new();
    readonly SemaphoreSlim _lane = new(1, 1);

    /// <inheritdoc/>
    public bool IsInBatch => _session.Value is not null;

    /// <inheritdoc/>
    public Task<T> InBatch<T>(Func<Task<T>> work) => IsInBatch ? work() : Run(_ => work(), CancellationToken.None);

    /// <inheritdoc/>
    public void AfterCommit(Action change)
    {
        if (_afterCommit.Value is { } pending)
        {
            pending.Add(change);
            return;
        }

        change();
    }

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
        InSession(session => collection.UpdateOneAsync(session, filter, update, options));

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
        InSession(session => collection.UpdateManyAsync(session, filter, update, options));

    /// <inheritdoc/>
    public Task<DeleteResult> DeleteOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
        InSession(session => collection.DeleteOneAsync(session, filter));

    /// <inheritdoc/>
    /// <remarks>
    /// A write error aborts the whole transaction, unlike an unfenced ordered bulk write, which keeps the requests before the failed one.
    /// To keep that contract the requests before the failed index are written again in a batch of their own before the error is rethrown.
    /// </remarks>
    public async Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken)
    {
        if (_session.Value is { } current)
        {
            await collection.BulkWriteAsync(current, requests, cancellationToken: cancellationToken);
            return;
        }

        try
        {
            await Run(session => collection.BulkWriteAsync(session, requests, cancellationToken: cancellationToken), cancellationToken);
        }
        catch (MongoBulkWriteException<BsonDocument> error) when (error.WriteErrors.Count > 0)
        {
            var failedAt = error.WriteErrors.Min(writeError => writeError.Index);
            if (failedAt > 0)
            {
                var written = requests.Take(failedAt).ToList();
                await Run(session => collection.BulkWriteAsync(session, written, cancellationToken: cancellationToken), cancellationToken);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _lane.Dispose();

    Task<T> InSession<T>(Func<IClientSessionHandle, Task<T>> write) =>
        _session.Value is { } current ? write(current) : Run(write, CancellationToken.None);

    async Task<T> Run<T>(Func<IClientSessionHandle, Task<T>> work, CancellationToken cancellationToken)
    {
        var token = await tokens.Current(cancellationToken);
        List<Action> committed = [];
        await _lane.WaitAsync(cancellationToken);
        try
        {
            var result = await executor.Execute(
                token,
                async session =>
                {
                    // Each attempt starts from nothing: changes queued by an attempt that aborted are dropped with it.
                    committed = [];
                    _session.Value = session;
                    _afterCommit.Value = committed;
                    try
                    {
                        return await work(session);
                    }
                    finally
                    {
                        _session.Value = null;
                        _afterCommit.Value = null;
                    }
                },
                cancellationToken);
            committed.ForEach(change => change());
            return result;
        }
        finally
        {
            _lane.Release();
        }
    }
}

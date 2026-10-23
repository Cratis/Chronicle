// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents sink writes admitted by the owner epoch, bounded by distinct destination documents.
/// </summary>
/// <param name="tokens">The current owner's token source.</param>
/// <param name="executor">The fenced transaction executor.</param>
/// <param name="maxBatchSize">The maximum number of destination documents in one transaction.</param>
public sealed partial class FencedSinkWrites(IFenceTokenSource tokens, IFencedBatchExecutor executor, int maxBatchSize) : ISinkWrites, IDisposable
{
    readonly AsyncLocal<IClientSessionHandle?> _session = new();
    readonly AsyncLocal<SinkBatchDocuments?> _documents = new();
    readonly AsyncLocal<List<Action>?> _afterCommit = new();
    readonly AsyncLocal<bool> _splitting = new();
    readonly SemaphoreSlim _lane = new(1, 1);
    readonly SemaphoreSlim _operation = new(1, 1);
    readonly int _cap = maxBatchSize > 0 ? maxBatchSize : throw new ArgumentOutOfRangeException(nameof(maxBatchSize));

    /// <summary>
    /// Initializes a new instance of the <see cref="FencedSinkWrites"/> class with the default document cap.
    /// </summary>
    /// <param name="tokens">The current owner's token source.</param>
    /// <param name="executor">The transaction executor.</param>
    public FencedSinkWrites(IFenceTokenSource tokens, IFencedBatchExecutor executor)
        : this(tokens, executor, 500)
    {
    }

    /// <inheritdoc/>
    public int? MaxBatchSize => _cap;

    /// <inheritdoc/>
    public bool IsInBatch => _session.Value is not null || _splitting.Value;

    /// <inheritdoc/>
    public async Task<T> InBatch<T>(Func<Task<T>> work)
    {
        if (IsInBatch)
        {
            return await work();
        }
        try
        {
            return await Run(_ => work(), CancellationToken.None);
        }
        catch (SinkBatchLimitExceeded)
        {
            // The oversized attempt has aborted. Only idempotent writes may enter the split path.
            _splitting.Value = true;
            _afterCommit.Value = [];
            try
            {
                var result = await work();
                _afterCommit.Value.ForEach(change => change());
                return result;
            }
            finally
            {
                _splitting.Value = false;
                _afterCommit.Value = null;
            }
        }
    }

    /// <inheritdoc/>
    public void AfterCommit(Action change)
    {
        if (_afterCommit.Value is { } pending)
        {
            pending.Add(change);
        }
        else
        {
            change();
        }
    }

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
        Write(collection, filter, false, options.IsUpsert, update, session => collection.UpdateOneAsync(session, filter, update, options));

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options)
    {
        if (!IsInBatch)
        {
            return InBatch(() => UpdateMany(collection, filter, update, options));
        }

        return _splitting.Value && _session.Value is null
            ? SplitUpdate(collection, filter, update, options)
            : Write(collection, filter, true, options.IsUpsert, update, session => collection.UpdateManyAsync(session, filter, update, options));
    }

    /// <inheritdoc/>
    public Task<DeleteResult> DeleteOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
        Write(collection, filter, false, false, null, session => collection.DeleteOneAsync(session, filter));

    /// <inheritdoc/>
    public Task<DeleteResult> DeleteMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter)
    {
        if (!IsInBatch)
        {
            return InBatch(() => DeleteMany(collection, filter));
        }

        return _splitting.Value && _session.Value is null
            ? SplitDelete(collection, filter)
            : Write(collection, filter, true, false, null, session => collection.DeleteManyAsync(session, filter));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _lane.Dispose();
        _operation.Dispose();
    }

    async Task<T> Write<T>(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool many, bool upsert, UpdateDefinition<BsonDocument>? update, Func<IClientSessionHandle, Task<T>> write)
    {
        if (_splitting.Value && update is not null)
        {
            SinkBatchDocuments.RequireRetrySafe(collection, update);
        }
        if (_splitting.Value && !many)
        {
            SinkBatchDocuments.RequireDestinationKey(collection, filter);
        }

        if (_session.Value is null)
        {
            return _splitting.Value
                ? await Run(_ => Write(collection, filter, many, upsert, update, write), CancellationToken.None)
                : await InBatch(() => Write(collection, filter, many, upsert, update, write));
        }

        await _operation.WaitAsync();
        try
        {
            await _documents.Value!.Reserve(collection, _session.Value, filter, many, upsert, CancellationToken.None);
            return await write(_session.Value);
        }
        finally
        {
            _operation.Release();
        }
    }

    async Task<UpdateResult> SplitUpdate(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options)
    {
        SinkBatchDocuments.RequireRetrySafe(collection, update);
        if (options.IsUpsert)
        {
            throw new InvalidOperationException("Split fenced multi-document upserts require a destination key.");
        }
        long matched = 0, modified = 0;
        foreach (var chunk in (await SinkBatchDocuments.FindIds(collection, null, filter, null, CancellationToken.None)).Chunk(_cap))
        {
            var selected = filter & Builders<BsonDocument>.Filter.In("_id", chunk);
            var result = await Run(_ => UpdateMany(collection, selected, update, options), CancellationToken.None);
            matched += result.MatchedCount;
            modified += result.ModifiedCount;
        }
        return new UpdateResult.Acknowledged(matched, modified, null);
    }

    async Task<DeleteResult> SplitDelete(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter)
    {
        long deleted = 0;
        foreach (var chunk in (await SinkBatchDocuments.FindIds(collection, null, filter, null, CancellationToken.None)).Chunk(_cap))
        {
            var selected = filter & Builders<BsonDocument>.Filter.In("_id", chunk);
            var result = await Run(_ => DeleteMany(collection, selected), CancellationToken.None);
            deleted += result.DeletedCount;
        }
        return new DeleteResult.Acknowledged(deleted);
    }

    async Task<T> Run<T>(Func<IClientSessionHandle, Task<T>> work, CancellationToken cancellationToken)
    {
        var token = await tokens.Current(cancellationToken);
        var parentCallbacks = _afterCommit.Value;
        List<Action> committed = [];
        await _lane.WaitAsync(cancellationToken);
        try
        {
            var result = await executor.Execute(
                token,
                async session =>
                {
                    committed = [];
                    _session.Value = session;
                    _documents.Value = new SinkBatchDocuments(_cap);
                    _afterCommit.Value = committed;
                    try
                    {
                        return await work(session);
                    }
                    finally
                    {
                        _session.Value = null;
                        _documents.Value = null;
                        _afterCommit.Value = parentCallbacks;
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

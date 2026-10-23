// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// One sink writer flushing batches of upserts into a collection, fenced or not, through the same <see cref="ISinkWrites"/> the
/// MongoDB sink uses.
/// </summary>
internal sealed class FenceCostWriter : IFencedBatchProbe, IDisposable
{
    const int KeySpace = 20_000;
    static readonly string _payload = new('x', 100);
    readonly ISinkWrites _writes;
    readonly IMongoCollection<BsonDocument> _collection;
    readonly string _prefix;
    long _attempts;
    long _batch;

    FenceCostWriter(IMongoCollection<BsonDocument> collection, string prefix, Func<FenceCostWriter, ISinkWrites> writes)
    {
        _collection = collection;
        _prefix = prefix;
        _writes = writes(this);
    }

    /// <summary>
    /// Gets how many fenced batch attempts were made, including retries.
    /// </summary>
    internal long Attempts => Interlocked.Read(ref _attempts);

    /// <inheritdoc/>
    public Task BeforeAdmission(FenceBatchId batchId)
    {
        Interlocked.Increment(ref _attempts);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AfterAdmission(FenceBatchId batchId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeforeCommit(FenceBatchId batchId) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => (_writes as IDisposable)?.Dispose();

    /// <summary>
    /// Creates a writer.
    /// </summary>
    /// <param name="fenced">Whether the writer is fenced.</param>
    /// <param name="database">The database holding the collection and the fence collections.</param>
    /// <param name="collection">The collection to write to.</param>
    /// <param name="writer">The writer's index, which is also its materialization key.</param>
    /// <returns>The writer.</returns>
    internal static async Task<FenceCostWriter> Create(bool fenced, IMongoDatabase database, IMongoCollection<BsonDocument> collection, int writer)
    {
        if (!fenced)
        {
            return new(collection, $"w{writer}", _ => UnfencedSinkWrites.Instance);
        }

        var token = await new FenceGates(database, FenceRetryPolicy.Default).Claim($"{collection.CollectionNamespace.CollectionName}/w{writer}", 1);
        return new(collection, $"w{writer}", probe => new FencedSinkWrites(new FixedFenceTokenSource(token), new FencedBatchExecutor(new MongoFenceStore(database, FenceRetryPolicy.Default), FenceRetryPolicy.Default, probe)));
    }

    /// <summary>
    /// Writes the next batch.
    /// </summary>
    /// <param name="size">The number of upserts in the batch.</param>
    /// <returns>Awaitable task.</returns>
    internal Task WriteBatch(int size)
    {
        var batch = Interlocked.Increment(ref _batch);
        var requests = new List<WriteModel<BsonDocument>>(size);
        for (var index = 0; index < size; index++)
        {
            var key = $"{_prefix}-{((batch * size) + index) % KeySpace}";
            requests.Add(new UpdateOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", key),
                Builders<BsonDocument>.Update.Set("batch", batch).Set("payload", _payload)) { IsUpsert = true });
        }

        return _writes.BulkWrite(_collection, requests, CancellationToken.None);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines how the MongoDB sink executes its data writes: directly, or fenced by the owner epoch of its generation.
/// </summary>
/// <remarks>
/// Every data write the sink makes goes through this seam. <see cref="UnfencedSinkWrites"/> makes exactly the driver calls the sink
/// always made; <see cref="FencedSinkWrites"/> runs each write, or each group of writes in <see cref="InBatch{T}"/>, inside a
/// transaction that also writes the gate record expecting the owner's epoch.
/// </remarks>
public interface ISinkWrites
{
    /// <summary>
    /// Gets a value indicating whether the current flow is inside a batch, where writes must be executed directly rather than buffered.
    /// </summary>
    bool IsInBatch { get; }

    /// <summary>
    /// Runs a group of writes atomically within the configured cap; larger fenced groups use retry-safe capped transactions.
    /// </summary>
    /// <typeparam name="T">The type of result.</typeparam>
    /// <param name="work">The writes; may be called again from the start after a transient transaction error.</param>
    /// <returns>The result of the work.</returns>
    Task<T> InBatch<T>(Func<Task<T>> work);

    /// <summary>
    /// Runs an in-memory change that must only take effect once the writes it describes are persisted.
    /// </summary>
    /// <param name="change">The change, typically to a cache the sink answers reads from.</param>
    /// <remarks>
    /// Outside a batch the change runs at once. Inside one it runs after the batch commits, and is dropped if the attempt that
    /// queued it aborts, so a revoked or failed batch cannot leave a cache describing state that never reached the collection.
    /// </remarks>
    void AfterCommit(Action change);

    /// <summary>
    /// Updates one document.
    /// </summary>
    /// <param name="collection">The collection to write.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="update">The update.</param>
    /// <param name="options">The options.</param>
    /// <returns>The <see cref="UpdateResult"/>.</returns>
    Task<UpdateResult> UpdateOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options);

    /// <summary>
    /// Updates every matching document.
    /// </summary>
    /// <param name="collection">The collection to write.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="update">The update.</param>
    /// <param name="options">The options.</param>
    /// <returns>The <see cref="UpdateResult"/>.</returns>
    Task<UpdateResult> UpdateMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options);

    /// <summary>
    /// Deletes one document.
    /// </summary>
    /// <param name="collection">The collection to write.</param>
    /// <param name="filter">The filter.</param>
    /// <returns>The <see cref="DeleteResult"/>.</returns>
    Task<DeleteResult> DeleteOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter);

    /// <summary>
    /// Deletes every matching document.
    /// </summary>
    /// <param name="collection">The collection to write.</param>
    /// <param name="filter">The filter.</param>
    /// <returns>The <see cref="DeleteResult"/>.</returns>
    Task<DeleteResult> DeleteMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter);

    /// <summary>
    /// Executes an ordered bulk write.
    /// </summary>
    /// <param name="collection">The collection to write.</param>
    /// <param name="requests">The write models, in order.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="MongoBulkWriteException{T}">A write failed; every request before the first failed index has been written.</exception>
    Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken);
}

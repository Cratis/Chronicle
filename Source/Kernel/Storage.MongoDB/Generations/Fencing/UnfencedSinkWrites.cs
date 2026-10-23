// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an <see cref="ISinkWrites"/> making exactly the driver calls the sink makes without a fence.
/// </summary>
public sealed class UnfencedSinkWrites : ISinkWrites
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly UnfencedSinkWrites Instance = new();

    /// <inheritdoc/>
    public bool IsInBatch => false;

    /// <inheritdoc/>
    public Task<T> InBatch<T>(Func<Task<T>> work) => work();

    /// <inheritdoc/>
    public void AfterCommit(Action change) => change();

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
        collection.UpdateOneAsync(filter, update, options);

    /// <inheritdoc/>
    public Task<UpdateResult> UpdateMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
        collection.UpdateManyAsync(filter, update, options);

    /// <inheritdoc/>
    public Task<DeleteResult> DeleteOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
        collection.DeleteOneAsync(filter);

    /// <inheritdoc/>
    public Task<DeleteResult> DeleteMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
        collection.DeleteManyAsync(filter);

    /// <inheritdoc/>
    public Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken) =>
        collection.BulkWriteAsync(requests, cancellationToken: cancellationToken);
}

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
    public int? MaxBatchSize => null;

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
        BulkWrite(collection, requests, null, cancellationToken);

    /// <inheritdoc/>
    public async Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, IReadOnlySet<WriteModel<BsonDocument>>? guardedUpserts, CancellationToken cancellationToken)
    {
        var remaining = requests.ToArray();
        while (remaining.Length > 0)
        {
            try
            {
                await collection.BulkWriteAsync(remaining, cancellationToken: cancellationToken);
                return;
            }
            catch (MongoBulkWriteException<BsonDocument> exception) when (IsRejectedGuardedUpsert(exception, remaining, guardedUpserts))
            {
                remaining = remaining.Skip(exception.WriteErrors[0].Index + 1).ToArray();
            }
        }
    }

    /// <summary>
    /// Determines whether an ordered batch stopped at an explicitly guarded upsert rejected by its primary key.
    /// </summary>
    /// <param name="exception">The ordered bulk failure.</param>
    /// <param name="requests">The submitted requests.</param>
    /// <param name="guardedUpserts">The explicitly guarded requests.</param>
    /// <returns>Whether the failed request is a watermark no-op.</returns>
    internal static bool IsRejectedGuardedUpsert(MongoBulkWriteException<BsonDocument> exception, IReadOnlyList<WriteModel<BsonDocument>> requests, IReadOnlySet<WriteModel<BsonDocument>>? guardedUpserts) =>
        exception.WriteConcernError is null && exception.WriteErrors.Count == 1 &&
        exception.WriteErrors[0] is { Category: ServerErrorCategory.DuplicateKey } error &&
        error.Message.Contains("index: _id_", StringComparison.Ordinal) &&
        error.Index >= 0 && error.Index < requests.Count &&
        guardedUpserts?.Contains(requests[error.Index]) == true;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an implementation of <see cref="IFenceGates"/> keeping gate records in a MongoDB database.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/> holding the gate records, the same database as the data they guard.</param>
/// <param name="retryPolicy">The <see cref="FenceRetryPolicy"/> bounding how long revocation waits for an in-flight batch.</param>
public class FenceGates(IMongoDatabase database, FenceRetryPolicy retryPolicy) : IFenceGates
{
    static readonly TransactionOptions _transactionOptions = new(ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority);
    readonly MongoDBGenerationGateWrites _gateWrites = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FenceGates"/> class with the default retry policy.
    /// </summary>
    /// <param name="database">The <see cref="IMongoDatabase"/> holding the gate records.</param>
    public FenceGates(IMongoDatabase database)
        : this(database, FenceRetryPolicy.Default)
    {
    }

    IMongoCollection<BsonDocument> Gates => database.GetCollection<BsonDocument>(FenceCollections.Gates);

    /// <inheritdoc/>
    public async Task<FenceToken> Claim(string materializationKey, ulong generation, CancellationToken cancellationToken = default)
    {
        await FenceStorage.EnsureCollections(database, cancellationToken);
        var epoch = await BumpEpoch(materializationKey, generation, cancellationToken);
        return new(materializationKey, generation, epoch);
    }

    /// <inheritdoc/>
    public async Task<long> Revoke(string materializationKey, ulong generation, CancellationToken cancellationToken = default)
    {
        await FenceStorage.EnsureCollections(database, cancellationToken);
        return await BumpEpoch(materializationKey, generation, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long?> CurrentEpoch(string materializationKey, ulong generation, CancellationToken cancellationToken = default)
    {
        var gateId = new FenceToken(materializationKey, generation, 0).GateId;
        using var cursor = await Gates.WithReadConcern(ReadConcern.Majority).FindAsync(Builders<BsonDocument>.Filter.Eq("_id", gateId), cancellationToken: cancellationToken);
        var gate = await cursor.FirstOrDefaultAsync(cancellationToken);
        return gate?[FenceCollections.Epoch].ToInt64();
    }

    async Task<long> BumpEpoch(string materializationKey, ulong generation, CancellationToken cancellationToken)
    {
        var gateId = new FenceToken(materializationKey, generation, 0).GateId;
        return await retryPolicy.RetryTransient(
            async () =>
            {
                using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
                session.StartTransaction(_transactionOptions);
                var epoch = await _gateWrites.Revoke(database, session, gateId, cancellationToken);
                await session.CommitTransactionAsync(cancellationToken);
                return epoch;
            },
            cancellationToken);
    }
}

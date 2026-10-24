// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an implementation of <see cref="IFenceStore"/> for a MongoDB replica set.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/> holding the gate and receipt records and the data they guard.</param>
/// <param name="retryPolicy">The <see cref="FenceRetryPolicy"/> bounding unknown-commit resolution.</param>
public class MongoFenceStore(IMongoDatabase database, FenceRetryPolicy retryPolicy) : IFenceStore
{
    /// <summary>
    /// The options every fenced transaction runs with: a snapshot read and a majority-acknowledged commit, so a committed batch
    /// survives failover and a resolution reads only what a majority holds.
    /// </summary>
    public static readonly TransactionOptions TransactionOptions = new(ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority);

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoFenceStore"/> class with the default retry policy.
    /// </summary>
    /// <param name="database">The <see cref="IMongoDatabase"/> holding the records.</param>
    public MongoFenceStore(IMongoDatabase database)
        : this(database, FenceRetryPolicy.Default)
    {
    }

    /// <inheritdoc/>
    public async Task<IFenceTransaction> Begin(CancellationToken cancellationToken)
    {
        var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction(TransactionOptions);
        return new MongoFenceTransaction(database, session);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The resolution writes the gate record unconditionally in its own transaction. That write conflicts with the unresolved
    /// batch for as long as the server holds it open, and once it commits the batch is settled for good: it either committed
    /// before, and its receipt is in the resolution's snapshot, or it can never commit. Its gate write was its first operation
    /// and had been acknowledged before the commit was sent, so it cannot arrive later. A conflict, or a resolution whose own
    /// commit is unknown, is retried; the resolution is idempotent.
    /// </remarks>
    public async Task<UnknownCommitResolution> Resolve(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ResolveOnce(token, batchId, cancellationToken);
            }
            catch (Exception error) when (FenceRetryPolicy.IsTransient(error) || FenceRetryPolicy.IsUnknownCommit(error))
            {
                if (attempt >= retryPolicy.MaxAttempts || DateTimeOffset.UtcNow - started >= retryPolicy.Deadline)
                {
                    throw new FencedBatchOutcomeUnknown(token, batchId, error);
                }

                await Task.Delay(retryPolicy.BackoffFor(attempt), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Decides what a settled resolution found.
    /// </summary>
    /// <param name="receiptExists">Whether the batch's receipt was in the resolution's snapshot.</param>
    /// <param name="gateEpoch">The epoch the gate held, or null if it no longer exists.</param>
    /// <param name="batchEpoch">The epoch the batch was admitted with.</param>
    /// <returns>The <see cref="UnknownCommitResolution"/>.</returns>
    internal static UnknownCommitResolution Decide(bool receiptExists, long? gateEpoch, long batchEpoch)
    {
        if (receiptExists)
        {
            return UnknownCommitResolution.Committed;
        }

        return gateEpoch == batchEpoch ? UnknownCommitResolution.NotCommitted : UnknownCommitResolution.Revoked;
    }

    async Task<UnknownCommitResolution> ResolveOnce(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction(TransactionOptions);
        var gate = await database.GetCollection<BsonDocument>(FenceCollections.Gates).FindOneAndUpdateAsync(
            session,
            Builders<BsonDocument>.Filter.Eq("_id", token.GateId),
            Builders<BsonDocument>.Update.Inc(FenceCollections.Resolutions, 1L),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        using var receipts = await database.GetCollection<BsonDocument>(FenceCollections.Receipts).FindAsync(
            session,
            Builders<BsonDocument>.Filter.Eq("_id", batchId.Value.ToString()),
            cancellationToken: cancellationToken);
        var receipt = await receipts.FirstOrDefaultAsync(cancellationToken);
        await session.CommitTransactionAsync(cancellationToken);

        return Decide(receipt is not null, gate?[FenceCollections.Epoch].ToInt64(), token.Epoch);
    }
}

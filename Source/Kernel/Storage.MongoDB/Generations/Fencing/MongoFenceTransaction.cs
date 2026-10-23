// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an implementation of <see cref="IFenceTransaction"/> over a MongoDB session transaction.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/> holding the gate and receipt records.</param>
/// <param name="session">The <see cref="IClientSessionHandle"/> with the transaction started.</param>
public sealed class MongoFenceTransaction(IMongoDatabase database, IClientSessionHandle session) : IFenceTransaction
{
    /// <inheritdoc/>
    public IClientSessionHandle Session => session;

    /// <inheritdoc/>
    public async Task<bool> Admit(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken)
    {
        var result = await database.GetCollection<BsonDocument>(FenceCollections.Gates).UpdateOneAsync(
            session,
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", token.GateId),
                Builders<BsonDocument>.Filter.Eq(FenceCollections.Epoch, token.Epoch)),
            Builders<BsonDocument>.Update.Inc(FenceCollections.Admitted, 1L),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    /// <inheritdoc/>
    public Task WriteReceipt(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken) =>
        database.GetCollection<BsonDocument>(FenceCollections.Receipts).InsertOneAsync(
            session,
            new BsonDocument
            {
                { "_id", batchId.Value.ToString() },
                { FenceCollections.Gate, token.GateId },
                { FenceCollections.Epoch, token.Epoch },
                { FenceCollections.At, DateTime.UtcNow }
            },
            cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task Commit(CancellationToken cancellationToken) => session.CommitTransactionAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task Abort()
    {
        if (!session.IsInTransaction)
        {
            return;
        }

        try
        {
            await session.AbortTransactionAsync();
        }
        catch (MongoException)
        {
            // The server has already aborted it, for instance after a write conflict; nothing is left to undo.
        }
    }

    /// <inheritdoc/>
    public void Dispose() => session.Dispose();
}

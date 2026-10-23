// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Prepares the collections the writer fence keeps its records in.
/// </summary>
public static class FenceStorage
{
    /// <summary>
    /// How long a batch receipt is kept; it only has to outlive the resolution of an unknown commit.
    /// </summary>
    public static readonly TimeSpan ReceiptRetention = TimeSpan.FromDays(1);

    /// <summary>
    /// Ensures the gate and receipt collections exist, outside any transaction, with receipts expiring.
    /// </summary>
    /// <param name="database">The <see cref="IMongoDatabase"/> to prepare.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Creating a collection implicitly inside a transaction is allowed from MongoDB 4.4, but doing it here keeps the first batch
    /// from racing another writer's first batch on the catalog, which aborts one of them.
    /// </remarks>
    public static async Task EnsureCollections(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        using var cursor = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var existing = await cursor.ToListAsync(cancellationToken);
        foreach (var name in new[] { FenceCollections.Gates, FenceCollections.Receipts }.Except(existing))
        {
            try
            {
                await database.CreateCollectionAsync(name, cancellationToken: cancellationToken);
            }
            catch (MongoCommandException error) when (error.CodeName == "NamespaceExists")
            {
                // Another writer created it first.
            }
        }

        var receipts = database.GetCollection<BsonDocument>(FenceCollections.Receipts);
        await receipts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(FenceCollections.At),
                new CreateIndexOptions { ExpireAfter = ReceiptRetention, Name = "fence_receipt_expiry" }),
            cancellationToken: cancellationToken);
    }
}

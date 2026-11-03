// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IResourceNameReservations"/>, using the uniqueness of <c language="csharp">_id</c>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> holding the cluster database.</param>
public class MongoDBResourceNameReservations(IDatabase database) : ResourceNameReservations
{
    const string OwnerField = "owner";

    /// <inheritdoc/>
    protected override async Task<string?> TryAdd(string key, string owner)
    {
        var collection = GetCollection();
        try
        {
            await collection.InsertOneAsync(new BsonDocument { { "_id", key }, { OwnerField, owner } });
            return null;
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", key)).FirstOrDefaultAsync();
            return existing?[OwnerField].AsString ?? string.Empty;
        }
    }

    /// <inheritdoc/>
    protected override async Task Remove(IReadOnlyCollection<string> keys, string owner)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var filter = Builders<BsonDocument>.Filter.In("_id", keys) & Builders<BsonDocument>.Filter.Eq(OwnerField, owner);
        await GetCollection().DeleteManyAsync(filter);
    }

    IMongoCollection<BsonDocument> GetCollection() => database.GetCollection<BsonDocument>(WellKnownCollectionNames.ResourceNameReservations);
}

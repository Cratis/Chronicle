// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>Stores durable tombstones in the event-store database, never the removable namespace database.</summary>
/// <param name="collection">The dedicated event-store-level lifecycle collection, without TTL indexes.</param>
public sealed class NamespaceLifecycleStorage(IMongoCollection<BsonDocument> collection) : INamespaceLifecycleStorage
{
    readonly IMongoCollection<BsonDocument> _collection = collection
        .WithWriteConcern(WriteConcern.WMajority)
        .WithReadConcern(ReadConcern.Majority)
        .WithReadPreference(ReadPreference.Primary);

    /// <inheritdoc/>
    public async Task<NamespaceLifecycle?> Get(EventStoreNamespaceName name)
    {
        var document = await _collection.Find(Id(name)).FirstOrDefaultAsync();
        return document is null ? null : NamespaceLifecycleDocument.Decode(document);
    }

    /// <inheritdoc/>
    public async Task<bool> TryWrite(EventStoreNamespaceName name, long? expectedRevision, NamespaceLifecycle next)
    {
        if (next.Revision != (expectedRevision ?? 0) + 1 || !name.Value.Equals(next.Namespace.Value, StringComparison.OrdinalIgnoreCase)) return false;
        var document = NamespaceLifecycleDocument.Encode(next);
        if (expectedRevision is null)
        {
            try
            {
                await _collection.InsertOneAsync(document);
                return true;
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
        var filter = Id(name) & Builders<BsonDocument>.Filter.Eq("revision", expectedRevision.Value);
        var result = await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false });
        return result.IsAcknowledged && result.MatchedCount == 1 && result.ModifiedCount == 1;
    }

    static FilterDefinition<BsonDocument> Id(EventStoreNamespaceName name) => Builders<BsonDocument>.Filter.Eq("_id", name.Value.ToUpperInvariant());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

public partial class EncryptionKeyStorage
{
    /// <inheritdoc/>
    public bool IsDurable => true;

    /// <inheritdoc/>
    public async Task<EncryptionKeyFenceState> ReadFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        try
        {
            var collection = ExternalFences(eventStore, eventStoreNamespace);
            var empty = EncryptionKeyFenceState.Empty.Serialize();
            try
            {
                var insert = new BsonDocument("$setOnInsert", new BsonDocument
                {
                    { "version", new Decimal128(0m) }, { "state", empty }, { "seal", EncryptionKeyFenceState.Seal(empty) }
                });
                await collection.WithWriteConcern(WriteConcern.WMajority).UpdateOneAsync(new BsonDocument("_id", identifier.Value), insert, new UpdateOptions { IsUpsert = true });
            }
            catch (MongoWriteException exception) when (IsDuplicate(exception))
            {
                // A definite non-write; read the winner linearly.
            }

            var row = await collection.WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary)
                .Find(new BsonDocument("_id", identifier.Value), new FindOptions { MaxTime = _readTimeout }).SingleOrDefaultAsync();
            if (row?.Contains("version") != true || !row.Contains("state") || !row.Contains("seal")) throw new EncryptionKeyFenceUnavailable(identifier);
            var version = Decimal128.ToDecimal(row["version"].AsDecimal128);
            if (decimal.Truncate(version) != version) throw new EncryptionKeyFenceUnavailable(identifier);
            return EncryptionKeyFenceState.Read(identifier, checked((ulong)version), row["state"].AsString, row["seal"].AsString);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException or OverflowException or InvalidCastException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CompareExchangeFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next)
    {
        observed.EnsureMonotonic(identifier, next);
        try
        {
            var state = next.Serialize();
            var result = await ExternalFences(eventStore, eventStoreNamespace).WithWriteConcern(WriteConcern.WMajority).UpdateOneAsync(
                new BsonDocument { { "_id", identifier.Value }, { "version", new Decimal128((decimal)observed.Erasure.Version) }, { "seal", EncryptionKeyFenceState.Seal(observed.Serialize()) } },
                new BsonDocument("$set", new BsonDocument
                {
                    { "version", new Decimal128((decimal)next.Erasure.Version) }, { "state", state }, { "seal", EncryptionKeyFenceState.Seal(state) }
                }));
            return result.MatchedCount == 1;
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    IMongoCollection<BsonDocument> ExternalFences(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace).GetCollection<BsonDocument>("EncryptionKeyExternalFences");
}

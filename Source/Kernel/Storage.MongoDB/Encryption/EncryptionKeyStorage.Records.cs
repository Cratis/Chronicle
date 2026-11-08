// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

public partial class EncryptionKeyStorage : IEncryptionKeyFenceRecords
{
    /// <inheritdoc/>
    public async Task<long?> CountLegacyFences(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        await GetErasureCollection(eventStore, eventStoreNamespace).WithReadPreference(ReadPreference.Primary).CountDocumentsAsync(_fences.Eq(_ => _.Seal, null), new CountOptions { MaxTime = _readTimeout });

    /// <inheritdoc/>
    public async Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id)
    {
        var row = await Records(eventStore, eventStoreNamespace).WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary)
            .Find(new BsonDocument("_id", id), new FindOptions { MaxTime = _readTimeout }).SingleOrDefaultAsync();
        if (row is null) return null;
        var value = row["value"].AsString;
        if (row["seal"].AsString != EncryptionKeyFenceState.Seal(value)) throw new InvalidOperationException("Invalid encryption key metadata seal.");
        return value;
    }

    /// <inheritdoc/>
    public async Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false)
    {
        try
        {
            await Records(eventStore, eventStoreNamespace).WithWriteConcern(WriteConcern.WMajority).UpdateOneAsync(
                new BsonDocument("_id", id),
                new BsonDocument(replace ? "$set" : "$setOnInsert", new BsonDocument { { "value", value }, { "seal", EncryptionKeyFenceState.Seal(value) } }),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (IsDuplicate(exception))
        {
            // The insert-only winner is validated by the archive caller before removing inline metadata.
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
        return observed.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ??
            await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId);
    }

    /// <inheritdoc/>
    public async Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            var next = await EncryptionKeyRequestArchive.Prepare(this, eventStore, eventStoreNamespace, identifier, observed, retain);
            if (await WriteFence(eventStore, eventStoreNamespace, EncryptionKeyFenceUpdates.Observed(identifier, observed), EncryptionKeyFenceUpdates.Compact(next))) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    IMongoCollection<BsonDocument> Records(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace).GetCollection<BsonDocument>("EncryptionKeyFenceRecords");
}

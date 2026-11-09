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
    public Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id) =>
        ReadRecord(eventStore, eventStoreNamespace, id, strong: true);

    /// <inheritdoc/>
    public async Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false)
    {
        try
        {
            await BoundedRecordWrite(cancellationToken => Records(eventStore, eventStoreNamespace).WithWriteConcern(StrongDurability).UpdateOneAsync(
                new BsonDocument("_id", id),
                new BsonDocument(replace ? "$set" : "$setOnInsert", new BsonDocument { { "value", value }, { "seal", EncryptionKeyFenceState.Seal(value) } }),
                new UpdateOptions { IsUpsert = true },
                cancellationToken));
        }
        catch (MongoWriteException exception) when (IsDuplicate(exception))
        {
            // The insert-only winner is validated by the archive caller before removing inline metadata.
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier, strong: true);
        return observed.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ??
            await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId);
    }

    /// <inheritdoc/>
    public async Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier, strong: true);
            var next = await EncryptionKeyRequestArchive.Prepare(this, eventStore, eventStoreNamespace, identifier, observed, retain);
            if (await WriteFence(eventStore, eventStoreNamespace, identifier, EncryptionKeyFenceUpdates.Observed(identifier, observed), EncryptionKeyFenceUpdates.Compact(next), strong: true)) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    async Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, bool strong)
    {
        var readConcern = strong ? ReadConcern.Linearizable : Linearizability;
        var row = await BoundedRecord(cancellationToken => Records(eventStore, eventStoreNamespace).WithReadConcern(readConcern).WithReadPreference(ReadPreference.Primary)
            .Find(new BsonDocument("_id", id), new FindOptions { MaxTime = _readTimeout }).SingleOrDefaultAsync(cancellationToken));
        if (row is null) return null;

        // Only an empty, non-confirming archive lookup may degrade. A found confirmation needs authoritative verification.
        if (readConcern != ReadConcern.Linearizable) return await ReadRecord(eventStore, eventStoreNamespace, id, strong: true);
        var value = row["value"].AsString;
        if (row["seal"].AsString != EncryptionKeyFenceState.Seal(value)) throw new InvalidOperationException("Invalid encryption key metadata seal.");
        return value;
    }

    IMongoCollection<BsonDocument> Records(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace).GetCollection<BsonDocument>("EncryptionKeyFenceRecords");

    sealed class PendingArchiveLookup(EncryptionKeyStorage storage) : IEncryptionKeyFenceRecords
    {
        public Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id) =>
            storage.ReadRecord(eventStore, eventStoreNamespace, id, strong: false);

        public Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false) =>
            storage.WriteRecord(eventStore, eventStoreNamespace, id, value, replace);
    }
}

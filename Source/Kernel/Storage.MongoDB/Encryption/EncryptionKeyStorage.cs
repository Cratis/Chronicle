// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EncryptionKeyStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IEventStoreDatabase"/> to use for accessing database.</param>
public class EncryptionKeyStorage(IDatabase database) : IEncryptionKeyStorage
{
    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var collection = GetCollection(eventStore, eventStoreNamespace);
        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(collection, identifier)
            : revision!;

        var id = new EncryptionKeyId(identifier, actualRevision);
        await collection.ReplaceOneAsync(
            _ => _.Id == id,
            new EncryptionKeyForIdentifier(id, key.Public, key.Private),
            new ReplaceOptions() { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = GetCollection(eventStore, eventStoreNamespace);
        if (IsLatest(revision))
        {
            return await collection.CountDocumentsAsync(_ => _.Id.Identifier == identifier) > 0;
        }

        var id = new EncryptionKeyId(identifier, revision!);
        return await collection.CountDocumentsAsync(_ => _.Id == id) == 1;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = GetCollection(eventStore, eventStoreNamespace);

        EncryptionKeyForIdentifier? keyDoc;
        if (IsLatest(revision))
        {
            keyDoc = await collection
                .Find(_ => _.Id.Identifier == identifier)
                .SortByDescending(_ => _.Id.Revision)
                .FirstOrDefaultAsync();
        }
        else
        {
            var id = new EncryptionKeyId(identifier, revision!);
            using var result = await collection.FindAsync(_ => _.Id == id);
            keyDoc = await result.SingleOrDefaultAsync();
        }

        ThrowIfMissingEncryptionKey(identifier, keyDoc);
        return new(keyDoc.PublicKey, keyDoc.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = GetCollection(eventStore, eventStoreNamespace);
        if (IsLatest(revision))
        {
            await collection.DeleteManyAsync(_ => _.Id.Identifier == identifier);
        }
        else
        {
            var id = new EncryptionKeyId(identifier, revision!);
            await collection.DeleteOneAsync(_ => _.Id == id);
        }
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static async Task<EncryptionKeyRevision> GetNextRevision(IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier)
    {
        var latestDoc = await collection
            .Find(_ => _.Id.Identifier == identifier)
            .SortByDescending(_ => _.Id.Revision)
            .FirstOrDefaultAsync();

        return latestDoc is null ? (EncryptionKeyRevision)1u : latestDoc.Id.Revision.Value + 1u;
    }

    void ThrowIfMissingEncryptionKey(EncryptionKeyIdentifier identifier, EncryptionKeyForIdentifier? key)
    {
        if (key == default)
        {
            throw new MissingEncryptionKey(identifier);
        }
    }

    IMongoCollection<EncryptionKeyForIdentifier> GetCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    {
        var eventStoreDatabase = database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace);
        return eventStoreDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys");
    }
}

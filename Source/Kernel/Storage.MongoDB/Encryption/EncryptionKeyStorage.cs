// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
    /// <summary>
    /// The encryption key collection name.
    /// </summary>
    internal const string CollectionName = "encryption-keys";

    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <summary>
    /// Gets the encryption key indexes.
    /// </summary>
    internal static IReadOnlyList<CreateIndexModel<EncryptionKeyForIdentifier>> Indexes { get; } =
    [
        new(
            Builders<EncryptionKeyForIdentifier>.IndexKeys.Ascending(_ => _.Id.Identifier).Descending(_ => _.Id.Revision),
            new CreateIndexOptions { Name = "id_identifier_id_revision", Background = true })
    ];

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(collection, identifier)
            : revision!;

        (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).EnsureCanSave(identifier, actualRevision, key);

        var id = new EncryptionKeyId(identifier, actualRevision);
        await collection.ReplaceOneAsync(
            _ => _.Id == id,
            new EncryptionKeyForIdentifier(id, key.Public, key.Private),
            new ReplaceOptions() { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var document = await collection.Find(_ => _.Id.Identifier == identifier)
            .SortByDescending(_ => _.Id.Revision)
            .FirstOrDefaultAsync();
        return document is null ? null : new(new(document.PublicKey, document.PrivateKey), document.Id.Revision);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (await TryGetLatestFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var revision = (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).RevisionForNewKey(identifier, key);
        var id = new EncryptionKeyId(identifier, revision);
        try
        {
            await collection.InsertOneAsync(new EncryptionKeyForIdentifier(id, key.Public, key.Private));
            return new(key, revision);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another provisioner created the key concurrently. Converge on the persisted key so every writer
            // encrypts under the same one — the insert on the primary is the ordering point, so this is race-safe
            // even when the pre-check above read a stale replica. A winner that cannot be read back was removed
            // concurrently; the candidate was never persisted, so it must never be handed back.
            var winner = await collection.Find(_ => _.Id == id).FirstOrDefaultAsync()
                ?? throw new EncryptionKeyProvisioningConflict(identifier, revision);
            return new(new(winner.PublicKey, winner.PrivateKey), winner.Id.Revision);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        if (IsLatest(revision))
        {
            return await collection.Find(_ => _.Id.Identifier == identifier).Limit(1).AnyAsync();
        }

        var id = new EncryptionKeyId(identifier, revision!);
        return await collection.Find(_ => _.Id == id).Limit(1).AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);

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

        return keyDoc == default ? null : new(keyDoc.PublicKey, keyDoc.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision)
            ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
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

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var collection = GetErasureCollection(eventStore, eventStoreNamespace);
        var document = await collection.Find(_ => _.Id == identifier).FirstOrDefaultAsync();
        return document is null
            ? null
            : new EncryptionKeyErasure(document.ErasedThrough, document.ErasedKeyFingerprints, document.NewKeyAllowed);
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var keys = await GetCollection(eventStore, eventStoreNamespace);
        var present = await keys.Find(_ => _.Id.Identifier == identifier).ToListAsync();
        var erasure = EncryptionKeyErasure.Covering(
            await GetErasureFor(eventStore, eventStoreNamespace, identifier),
            present.Select(_ => (_.Id.Revision, new EncryptionKey(_.PublicKey, _.PrivateKey))));

        await UpsertErasure(eventStore, eventStoreNamespace, identifier, erasure);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        if (await GetErasureFor(eventStore, eventStoreNamespace, identifier) is not { } erasure)
        {
            return;
        }

        await UpsertErasure(eventStore, eventStoreNamespace, identifier, erasure with { NewKeyAllowed = true });
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

    async Task UpsertErasure(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasure erasure)
    {
        var collection = GetErasureCollection(eventStore, eventStoreNamespace);
        await collection.ReplaceOneAsync(
            _ => _.Id == identifier,
            new EncryptionKeyErasureForIdentifier(identifier, erasure.ErasedThrough, [.. erasure.ErasedKeyFingerprints], erasure.NewKeyAllowed),
            new ReplaceOptions { IsUpsert = true });
    }

    IMongoCollection<EncryptionKeyErasureForIdentifier> GetErasureCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        database
            .GetEventStoreDatabase(eventStore)
            .GetNamespaceDatabase(eventStoreNamespace)
            .GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures");

    async Task<IMongoCollection<EncryptionKeyForIdentifier>> GetCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    {
        var eventStoreDatabase = database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace);
        var collection = eventStoreDatabase.GetCollection<EncryptionKeyForIdentifier>(CollectionName);
        await collection.EnsureIndexesOnceAsync(_ensuredIndexes, Indexes.ToArray()).ConfigureAwait(false);
        return collection;
    }
}

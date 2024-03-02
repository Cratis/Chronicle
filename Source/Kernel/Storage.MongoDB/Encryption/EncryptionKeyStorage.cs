// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.Compliance;
using Aksio.Cratis.Kernel.Storage.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Compliance.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for MongoDB.
/// </summary>
public class EncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="database"><see cref="IEventStoreDatabase"/> to use for accessing database.</param>
    public EncryptionKeyStorage(IDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key)
    {
        await GetCollection(eventStore, eventStoreNamespace).ReplaceOneAsync(
            _ => _.Identifier == identifier,
            new EncryptionKeyForIdentifier(identifier, key.Public, key.Private),
            new ReplaceOptions() { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var result = await GetCollection(eventStore, eventStoreNamespace).CountDocumentsAsync(_ => _.Identifier == identifier);
        return result == 1;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var result = await GetCollection(eventStore, eventStoreNamespace).FindAsync(_ => _.Identifier == identifier);
        var key = result.SingleOrDefault();
        ThrowIfMissingEncryptionKey(identifier, key);
        return new(key.PublicKey, key.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) => await GetCollection(eventStore, eventStoreNamespace).DeleteOneAsync(_ => _.Identifier == identifier);

    void ThrowIfMissingEncryptionKey(EncryptionKeyIdentifier identifier, EncryptionKeyForIdentifier key)
    {
        if (key == default)
        {
            throw new MissingEncryptionKey(identifier);
        }
    }

    IMongoCollection<EncryptionKeyForIdentifier> GetCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    {
        var database = _database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace);
        return database.GetCollection<EncryptionKeyForIdentifier>("encryption-keys");
    }
}

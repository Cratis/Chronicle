// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        var eventStoreDatabase = database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace);
        return eventStoreDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys");
    }
}

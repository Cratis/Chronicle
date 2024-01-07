// Copyright (c) Aksio Insurtech. All rights reserved.
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
    readonly IEventStoreDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="database"><see cref="IEventStoreDatabase"/> to use for accessing database.</param>
    public EncryptionKeyStorage(IEventStoreDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        await GetCollection().ReplaceOneAsync(
            _ => _.Identifier == identifier,
            new EncryptionKeyForIdentifier(identifier, key.Public, key.Private),
            new ReplaceOptions() { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EncryptionKeyIdentifier identifier)
    {
        var result = await GetCollection().CountDocumentsAsync(_ => _.Identifier == identifier);
        return result == 1;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier)
    {
        var result = await GetCollection().FindAsync(_ => _.Identifier == identifier);
        var key = result.SingleOrDefault();
        ThrowIfMissingEncryptionKey(identifier, key);
        return new(key.PublicKey, key.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EncryptionKeyIdentifier identifier) => await GetCollection().DeleteOneAsync(_ => _.Identifier == identifier);

    void ThrowIfMissingEncryptionKey(EncryptionKeyIdentifier identifier, EncryptionKeyForIdentifier key)
    {
        if (key == default)
        {
            throw new MissingEncryptionKey(identifier);
        }
    }

    IMongoCollection<EncryptionKeyForIdentifier> GetCollection() => _database.GetCollection<EncryptionKeyForIdentifier>("encryption-keys");
}

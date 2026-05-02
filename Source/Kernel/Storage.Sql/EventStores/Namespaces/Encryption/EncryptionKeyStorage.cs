// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;
using StoredEncryptionKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class EncryptionKeyStorage(IDatabase database) : IEncryptionKeyStorage
{
    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        StoredEncryptionKey key)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        await scope.DbContext.EncryptionKeys.Upsert(new EncryptionKey
        {
            Identifier = identifier.Value,
            PublicKey = key.Public,
            PrivateKey = key.Private
        });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        return await scope.DbContext.EncryptionKeys.AnyAsync(e => e.Identifier == identifier.Value);
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var entity = await scope.DbContext.EncryptionKeys.SingleOrDefaultAsync(e => e.Identifier == identifier.Value) ?? throw new MissingEncryptionKey(identifier);
        return new StoredEncryptionKey(entity.PublicKey, entity.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var entity = await scope.DbContext.EncryptionKeys.SingleOrDefaultAsync(e => e.Identifier == identifier.Value);
        if (entity is not null)
        {
            scope.DbContext.EncryptionKeys.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}

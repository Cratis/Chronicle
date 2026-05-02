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
        StoredEncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        uint actualRevision;
        if (IsLatest(revision))
        {
            actualRevision = await GetNextRevision(scope.DbContext, identifier);
        }
        else
        {
            actualRevision = revision!.Value;
        }

        await scope.DbContext.EncryptionKeys.Upsert(new EncryptionKey
        {
            Identifier = identifier.Value,
            Revision = actualRevision,
            PublicKey = key.Public,
            PrivateKey = key.Private
        });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        if (IsLatest(revision))
        {
            return await scope.DbContext.EncryptionKeys.AnyAsync(e => e.Identifier == identifier.Value);
        }

        return await scope.DbContext.EncryptionKeys.AnyAsync(e => e.Identifier == identifier.Value && e.Revision == revision!.Value);
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        EncryptionKey? entity;
        if (IsLatest(revision))
        {
            entity = await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == identifier.Value)
                .OrderByDescending(e => e.Revision)
                .FirstOrDefaultAsync() ?? throw new MissingEncryptionKey(identifier);
        }
        else
        {
            entity = await scope.DbContext.EncryptionKeys
                .SingleOrDefaultAsync(e => e.Identifier == identifier.Value && e.Revision == revision!.Value)
                ?? throw new MissingEncryptionKey(identifier);
        }

        return new StoredEncryptionKey(entity.PublicKey, entity.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        if (IsLatest(revision))
        {
            var entities = scope.DbContext.EncryptionKeys.Where(e => e.Identifier == identifier.Value);
            scope.DbContext.EncryptionKeys.RemoveRange(entities);
        }
        else
        {
            var entity = await scope.DbContext.EncryptionKeys
                .SingleOrDefaultAsync(e => e.Identifier == identifier.Value && e.Revision == revision!.Value);
            if (entity is not null)
            {
                scope.DbContext.EncryptionKeys.Remove(entity);
            }
        }

        await scope.DbContext.SaveChangesAsync();
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static async Task<uint> GetNextRevision(NamespaceDbContext dbContext, EncryptionKeyIdentifier identifier)
    {
        var maxRevision = await dbContext.EncryptionKeys
            .Where(e => e.Identifier == identifier.Value)
            .MaxAsync(e => (uint?)e.Revision);
        return (maxRevision ?? 0u) + 1u;
    }
}

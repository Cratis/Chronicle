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

        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(scope.DbContext, identifier)
            : revision!.Value;

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

        EncryptionKey? entity = IsLatest(revision)
            ? await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == identifier.Value)
                .OrderByDescending(e => e.Revision)
                .FirstOrDefaultAsync() ?? throw new MissingEncryptionKey(identifier)
            : await scope.DbContext.EncryptionKeys
                .SingleOrDefaultAsync(e => e.Identifier == identifier.Value && e.Revision == revision!.Value)
                ?? throw new MissingEncryptionKey(identifier);

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
            await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == identifier.Value)
                .ExecuteDeleteAsync();
        }
        else
        {
            await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == identifier.Value && e.Revision == revision!.Value)
                .ExecuteDeleteAsync();
        }
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

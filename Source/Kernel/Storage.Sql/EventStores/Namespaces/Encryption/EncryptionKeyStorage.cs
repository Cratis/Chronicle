// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;
using StoredEncryptionKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;
using StoredEncryptionKeyErasure = Cratis.Chronicle.Storage.Compliance.EncryptionKeyErasure;

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

        (await ErasureIn(scope.DbContext, identifier)).EnsureCanSave(identifier, actualRevision, key);

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
    public async Task<StoredEncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        StoredEncryptionKey key)
    {
        if (await TryGetFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var revision = (await ErasureIn(scope.DbContext, identifier)).RevisionForNewKey(identifier, key);
        try
        {
            scope.DbContext.EncryptionKeys.Add(new EncryptionKey
            {
                Identifier = identifier.Value,
                Revision = revision.Value,
                PublicKey = key.Public,
                PrivateKey = key.Private
            });
            await scope.DbContext.SaveChangesAsync();
            return key;
        }
        catch (DbUpdateException)
        {
            // Another provisioner inserted the same revision first (primary key violation on
            // Identifier + Revision). Converge on the persisted key so every writer encrypts under the same one.
            var winner = await TryGetFor(eventStore, eventStoreNamespace, identifier, revision);
            return winner ?? key;
        }
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
    public async Task<StoredEncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        var entity = IsLatest(revision)
            ? await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == identifier.Value)
                .OrderByDescending(e => e.Revision)
                .FirstOrDefaultAsync()
            : await scope.DbContext.EncryptionKeys
                .SingleOrDefaultAsync(e => e.Identifier == identifier.Value && e.Revision == revision!.Value);

        return entity is null ? null : new StoredEncryptionKey(entity.PublicKey, entity.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetFor(
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

    /// <inheritdoc/>
    public async Task<StoredEncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        return await ErasureIn(scope.DbContext, identifier);
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        var present = await scope.DbContext.EncryptionKeys
            .Where(e => e.Identifier == identifier.Value)
            .ToListAsync();

        var erasure = StoredEncryptionKeyErasure.Covering(
            await ErasureIn(scope.DbContext, identifier),
            present.Select(_ => ((EncryptionKeyRevision)_.Revision, new StoredEncryptionKey(_.PublicKey, _.PrivateKey))));

        await UpsertErasure(scope.DbContext, identifier, erasure);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        if (await ErasureIn(scope.DbContext, identifier) is not { } erasure)
        {
            return;
        }

        await UpsertErasure(scope.DbContext, identifier, erasure with { NewKeyAllowed = true });
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static async Task<uint> GetNextRevision(NamespaceDbContext dbContext, EncryptionKeyIdentifier identifier)
    {
        var maxRevision = await dbContext.EncryptionKeys
            .Where(e => e.Identifier == identifier.Value)
            .MaxAsync(e => (uint?)e.Revision);
        return (maxRevision ?? 0u) + 1u;
    }

    static async Task<StoredEncryptionKeyErasure?> ErasureIn(NamespaceDbContext dbContext, EncryptionKeyIdentifier identifier)
    {
        var entity = await dbContext.EncryptionKeyErasures
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Identifier == identifier.Value);

        return entity is null
            ? null
            : new StoredEncryptionKeyErasure(
                entity.ErasedThrough,
                entity.ErasedKeyFingerprints.Split(',', StringSplitOptions.RemoveEmptyEntries),
                entity.NewKeyAllowed);
    }

    static async Task UpsertErasure(NamespaceDbContext dbContext, EncryptionKeyIdentifier identifier, StoredEncryptionKeyErasure erasure)
    {
        await dbContext.EncryptionKeyErasures.Upsert(new EncryptionKeyErasure
        {
            Identifier = identifier.Value,
            ErasedThrough = erasure.ErasedThrough.Value,
            ErasedKeyFingerprints = string.Join(',', erasure.ErasedKeyFingerprints),
            NewKeyAllowed = erasure.NewKeyAllowed
        });
        await dbContext.SaveChangesAsync();
    }
}

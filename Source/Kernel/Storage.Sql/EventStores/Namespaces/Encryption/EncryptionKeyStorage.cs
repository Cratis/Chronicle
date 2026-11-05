// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoredEncryptionKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;
using StoredEncryptionKeyErasure = Cratis.Chronicle.Storage.Compliance.EncryptionKeyErasure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="logger">Optional <see cref="ILogger{EncryptionKeyStorage}"/> for logging.</param>
public class EncryptionKeyStorage(IDatabase database, ILogger<EncryptionKeyStorage>? logger = null) : IEncryptionKeyStorage
{
    readonly ILogger<EncryptionKeyStorage> _logger = logger ?? NullLogger<EncryptionKeyStorage>.Instance;
    readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName), bool> _reviewsReported = new();

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        StoredEncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);

        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(scope.DbContext, stored)
            : revision!.Value;

        (await ErasureIn(scope.DbContext, stored)).EnsureCanSave(identifier, actualRevision, key);

        await scope.DbContext.EncryptionKeys.Upsert(new EncryptionKey
        {
            Identifier = stored,
            Revision = actualRevision,
            PublicKey = key.Public,
            PrivateKey = key.Private
        });
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        StoredEncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        var entity = await scope.DbContext.EncryptionKeys
            .Where(e => e.Identifier == stored)
            .OrderByDescending(e => e.Revision)
            .FirstOrDefaultAsync();
        if (entity is null)
        {
            await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);
            return null;
        }

        return new(new StoredEncryptionKey(entity.PublicKey, entity.PrivateKey), entity.Revision);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, StoredEncryptionKey key)
    {
        if (await TryGetLatestFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        var revision = (await ErasureIn(scope.DbContext, stored)).RevisionForNewKey(identifier, key);
        try
        {
            scope.DbContext.EncryptionKeys.Add(new EncryptionKey
            {
                Identifier = stored,
                Revision = revision.Value,
                PublicKey = key.Public,
                PrivateKey = key.Private
            });
            await scope.DbContext.SaveChangesAsync();
            return new(key, revision);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Another provisioner inserted the same revision first (primary key violation on Identifier + Revision).
            // Converge on the persisted key so every writer encrypts under the same one. Any other write failure
            // propagates: nothing was persisted, so the candidate must never be handed back.
            var winner = await TryGetFor(eventStore, eventStoreNamespace, identifier, revision)
                ?? throw new EncryptionKeyProvisioningConflict(identifier, revision);
            SqlStorageTelemetry.RecoveredCollision("encryption-key");
            return new(winner, revision);
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
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        var has = IsLatest(revision)
            ? await scope.DbContext.EncryptionKeys.AnyAsync(e => e.Identifier == stored)
            : await scope.DbContext.EncryptionKeys.AnyAsync(e => e.Identifier == stored && e.Revision == revision!.Value);
        if (!has)
        {
            await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);
        }

        return has;
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        var entity = IsLatest(revision)
            ? await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == stored)
                .OrderByDescending(e => e.Revision)
                .FirstOrDefaultAsync()
            : await scope.DbContext.EncryptionKeys
                .SingleOrDefaultAsync(e => e.Identifier == stored && e.Revision == revision!.Value);

        if (entity is null)
        {
            await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);
            return null;
        }

        return new StoredEncryptionKey(entity.PublicKey, entity.PrivateKey);
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
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);

        if (IsLatest(revision))
        {
            await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == stored)
                .ExecuteDeleteAsync();
        }
        else
        {
            await scope.DbContext.EncryptionKeys
                .Where(e => e.Identifier == stored && e.Revision == revision!.Value)
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
        return await ErasureIn(scope.DbContext, EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier));
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);

        var present = await scope.DbContext.EncryptionKeys
            .Where(e => e.Identifier == stored)
            .ToListAsync();

        var erasure = StoredEncryptionKeyErasure.Covering(
            await ErasureIn(scope.DbContext, stored),
            present.Select(_ => ((EncryptionKeyRevision)_.Revision, new StoredEncryptionKey(_.PublicKey, _.PrivateKey))));

        await UpsertErasure(scope.DbContext, stored, erasure);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        await EnsureNotUnderReview(eventStore, eventStoreNamespace, scope.DbContext, identifier, stored);
        if (await ErasureIn(scope.DbContext, stored) is not { } erasure)
        {
            return;
        }

        await UpsertErasure(scope.DbContext, stored, erasure with { NewKeyAllowed = true });
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static async Task<uint> GetNextRevision(NamespaceDbContext dbContext, string stored)
    {
        var maxRevision = await dbContext.EncryptionKeys
            .Where(e => e.Identifier == stored)
            .MaxAsync(e => (uint?)e.Revision);
        return (maxRevision ?? 0u) + 1u;
    }

    static async Task<StoredEncryptionKeyErasure?> ErasureIn(NamespaceDbContext dbContext, string stored)
    {
        var entity = await dbContext.EncryptionKeyErasures
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Identifier == stored);

        return entity is null
            ? null
            : new StoredEncryptionKeyErasure(
                entity.ErasedThrough,
                entity.ErasedKeyFingerprints.Split(',', StringSplitOptions.RemoveEmptyEntries),
                entity.NewKeyAllowed);
    }

    static async Task UpsertErasure(NamespaceDbContext dbContext, string stored, StoredEncryptionKeyErasure erasure)
    {
        await dbContext.EncryptionKeyErasures.Upsert(new EncryptionKeyErasure
        {
            Identifier = stored,
            ErasedThrough = erasure.ErasedThrough.Value,
            ErasedKeyFingerprints = string.Join(',', erasure.ErasedKeyFingerprints),
            NewKeyAllowed = erasure.NewKeyAllowed
        });
    }

    async Task EnsureNotUnderReview(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        NamespaceDbContext dbContext,
        EncryptionKeyIdentifier identifier,
        string stored)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        if (_reviewsReported.TryAdd((eventStore, eventStoreNamespace), true))
        {
            var count = await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [EncryptionKeyIdentifierReviews]").SingleAsync();
            if (count > 0)
            {
                _logger.IdentifiersAwaitingReview(count);
            }
        }

        // The raw value is compared with the column's pre-upgrade collation, the encoded value with the exact one.
        var underReview = await dbContext.Database
            .SqlQuery<int>($"SELECT TOP(1) 1 AS [Value] FROM [EncryptionKeyIdentifierReviews] WHERE [LegacyIdentifier] = {identifier.Value} AND [Identifier] <> {stored}")
            .AnyAsync();
        if (underReview)
        {
            SqlStorageTelemetry.EncryptionKeyIdentifierRefused();
            _logger.IdentifierUnderReview();
            throw new EncryptionKeyIdentifierUnderReview();
        }
    }
}

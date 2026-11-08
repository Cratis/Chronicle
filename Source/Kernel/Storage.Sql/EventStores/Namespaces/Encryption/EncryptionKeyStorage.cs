// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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
    readonly SqlStorageTelemetry _telemetry = SqlStorageTelemetry.For(database);

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
        await EnsureNotUnderReview(scope.DbContext, identifier, stored);

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
            await EnsureNotUnderReview(scope.DbContext, identifier, stored);
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
            _telemetry.RecoveredCollision("encryption-key");
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
            await EnsureNotUnderReview(scope.DbContext, identifier, stored);
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
            await EnsureNotUnderReview(scope.DbContext, identifier, stored);
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
        await EnsureNotUnderReview(scope.DbContext, identifier, stored);

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
        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        var erasure = await ErasureIn(scope.DbContext, stored);
        if (erasure is null)
        {
            await EnsureNotUnderReview(scope.DbContext, identifier, stored);
        }

        return erasure;
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);

        var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
        await EnsureNotUnderReview(scope.DbContext, identifier, stored);

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
        await EnsureNotUnderReview(scope.DbContext, identifier, stored);
        if (await ErasureIn(scope.DbContext, stored) is not { } erasure)
        {
            return;
        }

        await UpsertErasure(scope.DbContext, stored, erasure with { NewKeyAllowed = true });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The secret is a row in the <see cref="WellKnownTableNames.InternalSecrets"/> table of the default namespace,
    /// keyed by the exact event store and purpose. The primary key is the ordering point: a concurrent creator that loses reads the winner.
    /// </remarks>
    public async Task<StoredEncryptionKey> GetOrAddInternalSecretFor(EventStoreName eventStore, InternalSecretPurpose purpose, StoredEncryptionKey candidate)
    {
        var storeBytes = Encoding.UTF8.GetBytes(eventStore.Value);
        await using var scope = await database.Namespace(eventStore, EventStoreNamespaceName.Default);
        if (await FindInternalSecret(scope.DbContext, storeBytes, purpose) is { } existing)
        {
            return existing;
        }

        try
        {
            scope.DbContext.InternalSecrets.Add(new InternalSecret
            {
                EventStore = storeBytes,
                Purpose = purpose.Value,
                PublicKey = candidate.Public,
                PrivateKey = candidate.Private
            });
            await scope.DbContext.SaveChangesAsync();
            return candidate;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            await using var retry = await database.Namespace(eventStore, EventStoreNamespaceName.Default);
            var winner = await FindInternalSecret(retry.DbContext, storeBytes, purpose) ?? throw new InternalSecretProvisioningConflict(purpose);
            _telemetry.RecoveredCollision("internal-secret");
            return winner;
        }
    }

    static async Task<StoredEncryptionKey?> FindInternalSecret(NamespaceDbContext dbContext, byte[] eventStore, InternalSecretPurpose purpose)
    {
        var row = await dbContext.InternalSecrets.AsNoTracking().SingleOrDefaultAsync(e => e.EventStore == eventStore && e.Purpose == purpose.Value);
        return row is null ? null : new StoredEncryptionKey(row.PublicKey, row.PrivateKey);
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
        NamespaceDbContext dbContext,
        EncryptionKeyIdentifier identifier,
        string stored)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        // The raw value is compared with the column's pre-upgrade collation, the encoded value with the exact one.
        var underReview = await dbContext.Database
            .SqlQuery<int>($"SELECT TOP(1) 1 AS [Value] FROM [EncryptionKeyIdentifierReviews] WHERE [LegacyIdentifier] = {identifier.Value} AND [Identifier] <> {stored}")
            .AnyAsync();
        if (underReview)
        {
            _telemetry.EncryptionKeyIdentifierRefused();
            _logger.IdentifierUnderReview();
            throw new EncryptionKeyIdentifierUnderReview();
        }
    }
}

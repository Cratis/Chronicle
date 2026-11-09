// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fence = Cratis.Chronicle.Storage.Compliance.EncryptionKeyErasure;
using StoredEncryptionKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="logger">Optional <see cref="ILogger{EncryptionKeyStorage}"/> for logging.</param>
/// <param name="random">Optional <see cref="Random"/> used for retry jitter; defaults to <see cref="Random.Shared"/>.</param>
/// <param name="delay">Optional delay function used between retries; defaults to <see cref="Task.Delay(TimeSpan)"/>.</param>
public partial class EncryptionKeyStorage(IDatabase database, ILogger<EncryptionKeyStorage>? logger = null, Random? random = null, Func<TimeSpan, Task>? delay = null) : IEncryptionKeyStorage, IEncryptionKeyFence
{
    const int MaximumAttempts = 8;
    const int MaximumRequests = 1024;
    readonly ILogger<EncryptionKeyStorage> _logger = logger ?? NullLogger<EncryptionKeyStorage>.Instance;
    readonly SqlStorageTelemetry _telemetry = SqlStorageTelemetry.For(database);

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave;

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        StoredEncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var initial = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var fence = attempt == 0 ? initial : await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (fence.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
            var maximum = IsLatest(revision)
                ? await scope.DbContext.EncryptionKeys.Where(_ => _.Identifier == stored).MaxAsync(_ => (uint?)_.Revision) ?? 0
                : 0;
            var next = IsLatest(revision) ? (EncryptionKeyRevision)checked(Math.Max(maximum, fence.ErasedThrough.Value) + 1) : revision!;
            fence.EnsureCanSave(identifier, next, key);
            fence.EnsureStillAllowed(identifier, new(key, next, initial.Lifecycle));
            try
            {
                await StageAndPublish(eventStore, eventStoreNamespace, identifier, key, next, fence, !IsLatest(revision));
                return;
            }
            catch (DbException exception) when (IsLatest(revision) && EncryptionKeyFenceWrites.IsDuplicate(exception))
            {
                await Backoff(attempt);
            }
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await ReadPublished(eventStore, eventStoreNamespace, identifier, null);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetAtRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision) =>
        ReadPublished(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, StoredEncryptionKey key)
    {
        var hit = await ReadPublished(eventStore, eventStoreNamespace, identifier, null);
        if (hit is not null) return hit;
        var initial = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var fence = attempt == 0 ? initial : await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (fence.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            var revision = fence.RevisionForNewKey(identifier, key);
            var existing = await ReadPublished(eventStore, eventStoreNamespace, identifier, null);
            if (existing is not null)
            {
                fence.EnsureStillAllowed(identifier, existing);
                return existing;
            }

            try
            {
                return await StageAndPublish(eventStore, eventStoreNamespace, identifier, key, revision, fence);
            }
            catch (DbException exception) when (EncryptionKeyFenceWrites.IsDuplicate(exception))
            {
                _telemetry.RecoveredCollision("encryption-key");
                await Backoff(attempt);
            }
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, StoredEncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        await ReadPublished(eventStore, eventStoreNamespace, identifier, revision) is not null;

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        (await ReadPublished(eventStore, eventStoreNamespace, identifier, revision))?.Key;

    /// <inheritdoc/>
    public async Task<StoredEncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var context = scope.DbContext;
        var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);
        await EnsureNotUnderReview(context, identifier, stored);
        string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
        var revisionFilter = IsLatest(revision) ? string.Empty : $" AND {N("Revision")} = {{1}}";
        await EncryptionKeyFenceWrites.Execute(context, $"DELETE FROM {N("EncryptionKeys")} WHERE {N("Identifier")} = {{0}}{revisionFilter}", stored, revision?.Value ?? 0);
    }

    /// <inheritdoc/>
    public async Task<Fence?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        try
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier);
            return row is null ? null : EncryptionKeyFenceWrites.Read(identifier, row);
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <inheritdoc/>
    public Task<Fence?> GetErasureForRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        GetFenceFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public async Task<Fence?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var row = await ReadFence(eventStore, eventStoreNamespace, identifier);
        if (row is null) return null;
        var fence = EncryptionKeyFenceWrites.Read(identifier, row);
        return fence.ErasedThrough.Value == 0 && fence.Requests?.Count == 0 && fence.ErasedKeyFingerprints.Count == 0 ? null : fence;
    }

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        RecordErasureFor(eventStore, eventStoreNamespace, identifier, new(Guid.NewGuid().ToString("N")));

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        var initial = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            var fence = EncryptionKeyFenceWrites.Read(identifier, row);
            if (fence.Requests!.Any(_ => _.RequestId == request.RequestId)) return;
            if (await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, request.RequestId) is not null)
            {
                var stamped = (fence with { Version = checked(fence.Version + 1) }).WithStateSeal();
                await using var archiveScope = await database.Namespace(eventStore, eventStoreNamespace);
                archiveScope.DbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
                if (await EncryptionKeyFenceWrites.CompareExchange(archiveScope.DbContext, identifier, row, stamped) == 1) return;
                await Backoff(attempt);
                continue;
            }

            if (fence.Lifecycle != (request.Lifecycle == 0 ? initial.Lifecycle : request.Lifecycle)) throw new EncryptionKeyFenceChanged(identifier);
            if (fence.Requests!.Count >= MaximumRequests) throw new EncryptionKeyRequestLogFull(identifier);
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            var stored = EncryptionKeyIdentifierColumn.Value(scope.DbContext, identifier);
            var present = await scope.DbContext.EncryptionKeys.AsNoTracking().Where(_ => _.Identifier == stored).ToListAsync();
            var next = Fence.Covering(fence, present.Select(_ => ((EncryptionKeyRevision)_.Revision, Key(_))));
            var recorded = new EncryptionKeyErasureRequest(request.RequestId, next.ErasedThrough, fence.Lifecycle, next.ErasedKeyFingerprints);
            next = (next with { Version = checked(fence.Version + 1), Requests = [.. fence.Requests, recorded] }).WithStateSeal();
            if (await EncryptionKeyFenceWrites.CompareExchange(scope.DbContext, identifier, row, next) == 1)
            {
                return;
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        if (fence?.NewKeyAllowed == false)
        {
            await AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, fence.Version, fence.Lifecycle);
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
        var fence = EncryptionKeyFenceWrites.Read(identifier, row);
        if (IsAuthorizationRetry(fence, observedVersion, observedLifecycle)) return;
        if (fence.NewKeyAllowed || fence.Version != observedVersion || fence.Lifecycle != observedLifecycle) throw new EncryptionKeyFenceChanged(identifier);
        var next = (fence with { Version = checked(fence.Version + 1), Lifecycle = checked(fence.Lifecycle + 1), NewKeyAllowed = true }).WithStateSeal();
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        if (await EncryptionKeyFenceWrites.CompareExchange(scope.DbContext, identifier, row, next) == 0)
        {
            var current = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (!IsAuthorizationRetry(current, observedVersion, observedLifecycle)) throw new EncryptionKeyFenceChanged(identifier);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            var fence = EncryptionKeyFenceWrites.Read(identifier, row);
            var request = fence.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            if (request.Confirmed) return;
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            var covered = await Covered(scope.DbContext, identifier, request).AsNoTracking().ToListAsync();
            var fingerprints = (request.Fingerprints ?? []).Concat(covered.Select(_ => Key(_).Fingerprint)).Distinct(StringComparer.Ordinal).ToArray();
            if (fingerprints.Length != (request.Fingerprints?.Count ?? 0))
            {
                var updated = request with { Fingerprints = fingerprints };
                var next = UpdateRequest(fence, updated) with { ErasedKeyFingerprints = fence.ErasedKeyFingerprints.Concat(fingerprints).Distinct(StringComparer.Ordinal).ToArray() };
                if (await EncryptionKeyFenceWrites.CompareExchange(scope.DbContext, identifier, row, next.WithStateSeal()) == 0)
                {
                    await Backoff(attempt);
                    continue;
                }
            }

            await Sweep(eventStore, eventStoreNamespace, identifier, request);
            return;
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            var fence = EncryptionKeyFenceWrites.Read(identifier, row);
            var request = fence.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            if (request.Confirmed) return;
            await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
            scope.DbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
            try
            {
                if (await Covered(scope.DbContext, identifier, request).AnyAsync()) throw new EncryptionKeyFenceUnavailable(identifier);
            }
            catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            if (request.Confirmed) return;
            var next = UpdateRequest(fence, request with { Confirmed = true }).WithStateSeal();
            if (await EncryptionKeyFenceWrites.CompareExchange(scope.DbContext, identifier, row, next) == 1) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
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

    /// <summary>
    /// Gets the bounded, jittered delay for a retry attempt: between the exponential base and twice the base (exclusive).
    /// </summary>
    /// <param name="attempt">The zero-based attempt.</param>
    /// <param name="random">The <see cref="Random"/> providing jitter.</param>
    /// <returns>The <see cref="TimeSpan"/> to wait.</returns>
    internal static TimeSpan BackoffDelay(int attempt, Random random)
    {
        var baseMilliseconds = Math.Min(1 << Math.Clamp(attempt, 0, 5), 32);
        return TimeSpan.FromMilliseconds(baseMilliseconds + random.Next(0, baseMilliseconds));
    }

    static Fence UpdateRequest(Fence fence, EncryptionKeyErasureRequest request) => fence with
    {
        Version = checked(fence.Version + 1),
        Requests = fence.Requests!.Select(_ => _.RequestId == request.RequestId ? request : _).ToArray()
    };

    static IQueryable<EncryptionKey> Covered(NamespaceDbContext context, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);
        var through = (request.Through ?? EncryptionKeyRevision.Initial).Value;
        return context.EncryptionKeys.Where(_ => _.Identifier == stored && (_.Revision <= through || (_.StagedLifecycle != null && _.StagedLifecycle <= request.Lifecycle)));
    }

    static bool IsAuthorizationRetry(Fence fence, ulong version, uint lifecycle) =>
        version < ulong.MaxValue && lifecycle < uint.MaxValue && fence.Version == version + 1 && fence.Lifecycle == lifecycle + 1 && fence.NewKeyAllowed;

    static StoredEncryptionKey Key(EncryptionKey key) => new(key.PublicKey, key.PrivateKey);

    static async Task<StoredEncryptionKey?> FindInternalSecret(NamespaceDbContext dbContext, byte[] eventStore, InternalSecretPurpose purpose)
    {
        var row = await dbContext.InternalSecrets.AsNoTracking().SingleOrDefaultAsync(e => e.EventStore == eventStore && e.Purpose == purpose.Value);
        return row is null ? null : new StoredEncryptionKey(row.PublicKey, row.PrivateKey);
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    Task Backoff(int attempt) => (delay ?? (_ => Task.Delay(_))).Invoke(BackoffDelay(attempt, random ?? Random.Shared));

    async Task<EncryptionKeyAtRevision> StageAndPublish(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, StoredEncryptionKey key, EncryptionKeyRevision revision, Fence initial, bool replacePublished = false)
    {
        var nonce = Guid.NewGuid().ToString("N");
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        await EncryptionKeyFenceWrites.Stage(scope.DbContext, identifier, revision.Value, key, initial.Lifecycle, nonce, replacePublished);
        var candidate = new EncryptionKeyAtRevision(key, revision, initial.Lifecycle);
        try
        {
            for (var attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                var fence = await RequiredFence(eventStore, eventStoreNamespace, identifier);
                if (fence.Version < initial.Version) throw new EncryptionKeyFenceUnavailable(identifier);
                fence.EnsureStillAllowed(identifier, candidate);
                if (await EncryptionKeyFenceWrites.Publish(scope.DbContext, identifier, revision.Value, nonce, fence) == 1)
                {
                    var after = await RequiredFence(eventStore, eventStoreNamespace, identifier);
                    after.EnsureStillAllowed(identifier, candidate);
                    return candidate;
                }

                await Backoff(attempt);
            }

            throw new EncryptionKeyFenceUnavailable(identifier);
        }
        catch (Exception exception) when (exception is EncryptionKeyErased or EncryptionKeyFenceUnavailable)
        {
            var context = scope.DbContext;
            string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
            await EncryptionKeyFenceWrites.Write(identifier, () => EncryptionKeyFenceWrites.Execute(
                context,
                $"DELETE FROM {N("EncryptionKeys")} WHERE {N("Identifier")} = {{0}} AND {N("Revision")} = {{1}} AND {N("WriteNonce")} = {{2}} AND {N("StagedLifecycle")} = {{3}}",
                [EncryptionKeyIdentifierColumn.Value(context, identifier), revision.Value, nonce, initial.Lifecycle]));
            throw;
        }
    }

    async Task<EncryptionKeyAtRevision?> ReadPublished(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision)
    {
        try
        {
            return await ReadPublishedCore(eventStore, eventStoreNamespace, identifier, revision);
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    async Task<EncryptionKeyAtRevision?> ReadPublishedCore(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var context = scope.DbContext;
        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
        var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);

        // One primary statement observes key publication and its fence together, preserving the single-command hit path.
        var keys = context.EncryptionKeys.AsNoTracking().Where(_ => _.Identifier == stored && (_.Published == null || _.Published == true));
        if (!IsLatest(revision)) keys = keys.Where(_ => _.Revision == revision!.Value);
        var result = await (from key in keys
                            join fence in context.EncryptionKeyErasures.AsNoTracking() on key.Identifier equals fence.Identifier into fences
                            from fence in fences.DefaultIfEmpty()
                            orderby key.Revision descending
                            select new { Key = key, Fence = fence }).FirstOrDefaultAsync();
        if (result is null)
        {
            await EnsureNotUnderReview(context, identifier, stored);
            return null;
        }

        var candidate = new EncryptionKeyAtRevision(Key(result.Key), result.Key.Revision, result.Key.StagedLifecycle);
        if (result.Fence is null)
        {
            if (result.Key.StagedLifecycle is not null) throw new EncryptionKeyFenceUnavailable(identifier);
        }
        else
        {
            var fence = EncryptionKeyFenceWrites.Read(identifier, result.Fence);
            if (!fence.NewKeyAllowed || fence.Covers(candidate)) return null;
        }

        return candidate;
    }

    async Task<Fence> EnsureFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        uint? lifecycle = null;
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier);
            if (row is not null)
            {
                var fence = EncryptionKeyFenceWrites.Read(identifier, row);
                lifecycle ??= fence.Lifecycle;
                if (fence.Lifecycle != lifecycle) throw new EncryptionKeyFenceChanged(identifier);
                if (row.Version is not null) return fence;
                await using var upgrade = await database.Namespace(eventStore, eventStoreNamespace);
                if (await EncryptionKeyFenceWrites.CompareExchange(upgrade.DbContext, identifier, row, fence) == 1) return fence;
            }
            else
            {
                var empty = new Fence(0, [], true).WithStateSeal();
                lifecycle ??= empty.Lifecycle;
                await using var insert = await database.Namespace(eventStore, eventStoreNamespace);
                if (await EncryptionKeyFenceWrites.InsertFence(insert.DbContext, identifier, empty)) return empty;
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    async Task<Fence> RequiredFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        try
        {
            var row = await ReadFence(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            return EncryptionKeyFenceWrites.Read(identifier, row);
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    async Task<EncryptionKeyErasure?> ReadFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var context = scope.DbContext;
        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));
        var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);
        try
        {
            // Fresh scoped contexts read the primary at the provider's default READ COMMITTED isolation (SQLite: serialized statements).
            var row = await context.EncryptionKeyErasures.AsNoTracking().SingleOrDefaultAsync(_ => _.Identifier == stored);
            if (row is null) await EnsureNotUnderReview(context, identifier, stored);
            return row;
        }
        catch (Exception exception) when (exception is DbException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    async Task Sweep(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        await using var scope = await database.Namespace(eventStore, eventStoreNamespace);
        var context = scope.DbContext;
        string N(string name) => EncryptionKeyFenceWrites.Name(context, name);
        await EncryptionKeyFenceWrites.Write(identifier, () => EncryptionKeyFenceWrites.Execute(
            context,
            $"DELETE FROM {N("EncryptionKeys")} WHERE {N("Identifier")} = {{0}} AND ({N("Revision")} <= {{1}} OR ({N("StagedLifecycle")} IS NOT NULL AND {N("StagedLifecycle")} <= {{2}}))",
            [EncryptionKeyIdentifierColumn.Value(context, identifier), (request.Through ?? EncryptionKeyRevision.Initial).Value, request.Lifecycle]));
    }

    async Task EnsureNotUnderReview(NamespaceDbContext dbContext, EncryptionKeyIdentifier identifier, string stored)
    {
        if (!dbContext.Database.IsSqlServer()) return;

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

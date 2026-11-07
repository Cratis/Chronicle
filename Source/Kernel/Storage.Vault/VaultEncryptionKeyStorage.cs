// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VaultSharp;
using VaultSharp.Core;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// HashiCorp Vault KV v2 encryption keys and request-bounded, CAS guarded erasure fences.
/// </summary>
public class VaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    const string DefaultMountPoint = "secret";
    const string CheckAndSetMismatch = "check-and-set parameter did not match";
    const int MaximumAttempts = 8;
    readonly IVaultClient _vaultClient;
    readonly string _mountPoint;
    readonly ILogger<VaultEncryptionKeyStorage> _logger;
    readonly VaultKeyStoreUpgradeCheck _layoutCheck;
    readonly VaultFenceAccess _access;
    volatile bool _layoutProven;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="connectionDetails">Vault server address.</param>
    /// <param name="mountPoint">KV v2 mount, defaulting to secret.</param>
    /// <param name="logger">Optional logger.</param>
    public VaultEncryptionKeyStorage(string connectionDetails, string? mountPoint = null, ILogger<VaultEncryptionKeyStorage>? logger = null)
        : this(VaultClients.Create(connectionDetails), mountPoint, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultEncryptionKeyStorage"/> class with an existing client.
    /// </summary>
    /// <param name="vaultClient">Vault client.</param>
    /// <param name="mountPoint">KV v2 mount, defaulting to secret.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="random">Optional retry jitter source.</param>
    /// <param name="delay">Optional retry delay.</param>
    internal VaultEncryptionKeyStorage(IVaultClient vaultClient, string? mountPoint = null, ILogger<VaultEncryptionKeyStorage>? logger = null, Random? random = null, Func<TimeSpan, Task>? delay = null)
    {
        _vaultClient = vaultClient;
        _mountPoint = string.IsNullOrEmpty(mountPoint) ? DefaultMountPoint : mountPoint;
        _logger = logger ?? NullLogger<VaultEncryptionKeyStorage>.Instance;
        _layoutCheck = new(vaultClient, _mountPoint);
        _access = new(vaultClient, _mountPoint, _layoutCheck, random, delay);
    }

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => _access.Guarantees;

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        var initial = await EnsureFence(locations, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var fence = attempt == 0 ? initial : (await RequiredFence(locations, identifier)).Fence;
            if (fence.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            var revisions = await ListRevisions(locations);
            var next = IsLatest(revision) ? (EncryptionKeyRevision)checked(Math.Max(revisions.Keys.Select(_ => _.Value).DefaultIfEmpty(0u).Max(), fence.ErasedThrough.Value) + 1) : revision!;
            fence.EnsureCanSave(identifier, next, key);
            fence.EnsureStillAllowed(identifier, new(key, next, initial.Lifecycle));
            var path = locations.Path(next);
            var observed = await _access.Read(path);
            if (observed is not null && ((!IsLatest(revision) && IsPublished(observed.Value.Data)) || IsTombstone(observed.Value.Data)))
            {
                if (IsTombstone(observed.Value.Data)) await _access.Destroy(path, observed.Value.Version - 1);
                if (await StageAndPublish(locations, identifier, key, next, fence, observed.Value.Version) is not null) return;
            }
            else if (observed is null && await StageAndPublish(locations, identifier, key, next, fence, 0) is not null)
            {
                return;
            }

            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        if (await ReadPublished(locations, identifier, null) is { } hit) return hit;
        var initial = await EnsureFence(locations, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var fence = attempt == 0 ? initial : (await RequiredFence(locations, identifier)).Fence;
            if (fence.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            var revision = fence.RevisionForNewKey(identifier, key);
            if (await ReadPublished(locations, identifier, null) is { } existing) return existing;
            var observed = await _access.Read(locations.Path(revision));
            if (observed is null || IsTombstone(observed.Value.Data))
            {
                if (observed is not null) await _access.Destroy(locations.Path(revision), observed.Value.Version - 1);
                if (await StageAndPublish(locations, identifier, key, revision, fence, observed?.Version ?? 0) is { } created) return created;
            }

            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await ReadPublished(await Resolve(eventStore, eventStoreNamespace, identifier), identifier, null);

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) is not null;

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        (await ReadPublished(await Resolve(eventStore, eventStoreNamespace, identifier), identifier, revision))?.Key;

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        await EnsureLayoutProven();
        await _access.Require(identifier);
        foreach (var (number, path) in await RevisionPaths(locations))
        {
            if (IsLatest(revision) || number == revision) await Tombstone(path, identifier, _ => true);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        (await ReadFence(await Resolve(eventStore, eventStoreNamespace, identifier), identifier))?.Fence;

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        return fence is null || (fence.ErasedThrough.Value == 0 && fence.Requests?.Count == 0 && fence.ErasedKeyFingerprints.Count == 0) ? null : fence;
    }

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        RecordErasureFor(eventStore, eventStoreNamespace, identifier, new(Guid.NewGuid().ToString("N")));

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        var initial = await EnsureFence(locations, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var (fence, version) = await RequiredFence(locations, identifier);
            if (fence.Requests!.Any(_ => _.RequestId == request.RequestId)) return;
            if (fence.Lifecycle != (request.Lifecycle == 0 ? initial.Lifecycle : request.Lifecycle)) throw new EncryptionKeyFenceChanged(identifier);
            if (fence.Requests!.Count >= 1024) throw new EncryptionKeyRequestLogFull(identifier);
            var present = await Present(locations);
            var covering = EncryptionKeyErasure.Covering(fence, present.Select(_ => (_.Revision, _.Key)));
            var captured = new EncryptionKeyErasureRequest(request.RequestId, covering.ErasedThrough, fence.Lifecycle, covering.ErasedKeyFingerprints, false);
            var next = covering with { Requests = fence.Requests.Append(captured).ToArray() };
            if (await _access.WriteFence(locations.ErasurePath, next, version) is not null) return;
            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        if (fence?.NewKeyAllowed == false) await AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, fence.Version, fence.Lifecycle);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        var (fence, version) = await RequiredFence(locations, identifier);
        if (IsAuthorizationRetry(fence, observedVersion, observedLifecycle)) return;
        if (fence.NewKeyAllowed || fence.Version != observedVersion || fence.Lifecycle != observedLifecycle) throw new EncryptionKeyFenceChanged(identifier);
        await EnsureLayoutProven();
        var next = fence with { Version = checked(fence.Version + 1), Lifecycle = checked(fence.Lifecycle + 1), NewKeyAllowed = true };
        if (await _access.WriteFence(locations.ErasurePath, next, version) is null && !IsAuthorizationRetry((await RequiredFence(locations, identifier)).Fence, observedVersion, observedLifecycle))
        {
            throw new EncryptionKeyFenceChanged(identifier);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        await EnsureLayoutProven();
        var request = Request((await RequiredFence(locations, identifier)).Fence, identifier, requestId);
        foreach (var (revision, path) in await RevisionPaths(locations))
        {
            await Tombstone(path, identifier, data => Covered(request, revision, data), async data =>
            {
                if (Key(data) is { } key) await AddFingerprint(locations, identifier, requestId, key.Fingerprint);
            });
        }
    }

    /// <inheritdoc/>
    public async Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var locations = await Resolve(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var (fence, version) = await RequiredFence(locations, identifier);
            var request = Request(fence, identifier, requestId);
            try
            {
                foreach (var (revision, path) in await RevisionPaths(locations))
                {
                    var row = await _access.Read(path);
                    if (row is null) continue;
                    if (Key(row.Value.Data) is not null && Covered(request, revision, row.Value.Data)) throw new EncryptionKeyFenceUnavailable(identifier);
                    if (IsTombstone(row.Value.Data) && !await _access.IsDestroyed(path, row.Value.Version - 1)) throw new EncryptionKeyFenceUnavailable(identifier);
                }
            }
            catch (Exception exception) when (exception is VaultApiException or HttpRequestException or TimeoutException or OperationCanceledException)
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            if (request.Confirmed) return;
            if (await _access.WriteFence(locations.ErasurePath, UpdateRequest(fence, request with { Confirmed = true }), version) is not null) return;
            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <summary>Identify a definite native CAS conflict.</summary>
    /// <param name="exception">The Vault response.</param>
    /// <returns>Whether the write definitely lost.</returns>
    internal static bool IsCheckAndSetMismatch(VaultApiException exception) =>
        exception.HttpStatusCode == HttpStatusCode.BadRequest &&
        ((exception.ApiErrors?.Any(error => error.Contains(CheckAndSetMismatch, StringComparison.OrdinalIgnoreCase)) ?? false) || exception.Message.Contains(CheckAndSetMismatch, StringComparison.OrdinalIgnoreCase));

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;
    static bool IsTombstone(Dictionary<string, object> data) => data.ContainsKey("erasedTombstone");
    static bool IsPublished(Dictionary<string, object> data) => !IsTombstone(data) && (!data.TryGetValue("published", out var value) || bool.Parse(value.ToString()!));
    static uint? Lifecycle(Dictionary<string, object> data) => data.TryGetValue("stagedLifecycle", out var value) ? uint.Parse(value.ToString()!, CultureInfo.InvariantCulture) : null;
    static EncryptionKey? Key(Dictionary<string, object> data) => IsTombstone(data) || !data.TryGetValue("publicKey", out var publicKey) || !data.TryGetValue("privateKey", out var privateKey)
        ? null : new(Convert.FromBase64String(publicKey.ToString()!), Convert.FromBase64String(privateKey.ToString()!));
    static bool Covered(EncryptionKeyErasureRequest request, EncryptionKeyRevision revision, Dictionary<string, object> data) =>
        revision.Value <= (request.Through ?? EncryptionKeyRevision.Initial).Value ||
        (Lifecycle(data) is { } lifecycle && lifecycle <= request.Lifecycle) ||
        (Key(data) is { } key && request.Fingerprints?.Contains(key.Fingerprint, StringComparer.Ordinal) == true);
    static bool IsAuthorizationRetry(EncryptionKeyErasure fence, ulong version, uint lifecycle) =>
        version < ulong.MaxValue && lifecycle < uint.MaxValue && fence.Version == version + 1 && fence.Lifecycle == lifecycle + 1 && fence.NewKeyAllowed;
    static EncryptionKeyErasureRequest Request(EncryptionKeyErasure fence, EncryptionKeyIdentifier identifier, string id) =>
        fence.Requests?.SingleOrDefault(_ => _.RequestId == id) ?? throw new EncryptionKeyFenceChanged(identifier);
    static EncryptionKeyErasure UpdateRequest(EncryptionKeyErasure fence, EncryptionKeyErasureRequest request) => fence with
    {
        Version = checked(fence.Version + 1),
        Requests = fence.Requests!.Select(_ => _.RequestId == request.RequestId ? request : _).ToArray()
    };

    async Task<EncryptionKeyAtRevision?> StageAndPublish(VaultKeyLocations locations, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision revision, EncryptionKeyErasure initial, int observedVersion)
    {
        var path = locations.Path(revision);
        var nonce = Guid.NewGuid().ToString("N");
        var data = new Dictionary<string, object>
        {
            ["publicKey"] = Convert.ToBase64String(key.Public),
            ["privateKey"] = Convert.ToBase64String(key.Private),
            ["layout"] = VaultPathSegments.Root,
            ["published"] = false,
            ["writerNonce"] = nonce,
            ["stagedLifecycle"] = initial.Lifecycle
        };
        var stagedVersion = await _access.Write(path, data, observedVersion);
        if (stagedVersion is null) return null;
        var candidate = new EncryptionKeyAtRevision(key, revision, initial.Lifecycle);
        var ownedVersion = stagedVersion.Value;
        try
        {
            var fence = (await RequiredFence(locations, identifier)).Fence;
            if (fence.Version < initial.Version) throw new EncryptionKeyFenceUnavailable(identifier);
            if (fence.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            fence.EnsureStillAllowed(identifier, candidate);
            data["published"] = true;
            var publishedVersion = await _access.Write(path, data, stagedVersion.Value);
            if (publishedVersion is null)
            {
                // Destruction alone does not invalidate CAS. The sweep's tombstone advances the version first.
                _ = await RequiredFence(locations, identifier);
                throw new EncryptionKeyFenceChanged(identifier);
            }

            ownedVersion = publishedVersion.Value;
            var released = (await RequiredFence(locations, identifier)).Fence;
            if (released.Version < fence.Version) throw new EncryptionKeyFenceUnavailable(identifier);
            if (released.Lifecycle != initial.Lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            released.EnsureStillAllowed(identifier, candidate);
            return candidate;
        }
        catch
        {
            await Tombstone(path, identifier, row => row.TryGetValue("writerNonce", out var writer) && writer.ToString() == nonce, expectedVersion: ownedVersion);
            throw;
        }
    }

    async Task Tombstone(string path, EncryptionKeyIdentifier identifier, Func<Dictionary<string, object>, bool> covered, Func<Dictionary<string, object>, Task>? beforeDestroy = null, int? expectedVersion = null)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var row = await _access.Read(path);
            if (row is null) return;
            var (data, version) = row.Value;
            if (expectedVersion is not null && expectedVersion != version) return;
            if (IsTombstone(data))
            {
                await _access.Destroy(path, version - 1);
                return;
            }

            if (!covered(data)) return;
            if (beforeDestroy is not null) await beforeDestroy(data);
            var tombstone = new Dictionary<string, object> { ["erasedTombstone"] = true, ["layout"] = VaultPathSegments.Root };
            if (await _access.Write(path, tombstone, version) is not null)
            {
                await _access.Destroy(path, version);
                return;
            }

            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    async Task AddFingerprint(VaultKeyLocations locations, EncryptionKeyIdentifier identifier, string requestId, string fingerprint)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var (fence, version) = await RequiredFence(locations, identifier);
            var request = Request(fence, identifier, requestId);
            if (request.Fingerprints?.Contains(fingerprint, StringComparer.Ordinal) == true) return;
            var updated = request with { Fingerprints = (request.Fingerprints ?? []).Append(fingerprint).Distinct(StringComparer.Ordinal).ToArray() };
            var next = UpdateRequest(fence, updated) with { ErasedKeyFingerprints = fence.ErasedKeyFingerprints.Append(fingerprint).Distinct(StringComparer.Ordinal).ToArray() };
            if (await _access.WriteFence(locations.ErasurePath, next, version) is not null) return;
            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    async Task<EncryptionKeyAtRevision?> ReadPublished(VaultKeyLocations locations, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision)
    {
        var paths = await ListRevisions(locations);
        foreach (var (number, path) in paths.OrderByDescending(_ => _.Key.Value))
        {
            if (!IsLatest(revision) && number != revision) continue;
            var row = await _access.Read(path);
            if (row is null || !IsPublished(row.Value.Data) || Key(row.Value.Data) is not { } key) continue;
            var candidate = new EncryptionKeyAtRevision(key, number, Lifecycle(row.Value.Data));
            var fence = (await ReadFence(locations, identifier))?.Fence;
            if (candidate.StagedLifecycle is not null && fence is null) throw new EncryptionKeyFenceUnavailable(identifier);
            if (fence is null || (fence.NewKeyAllowed && !fence.Covers(candidate))) return candidate;
        }

        return null;
    }

    async Task<EncryptionKeyErasure> EnsureFence(VaultKeyLocations locations, EncryptionKeyIdentifier identifier)
    {
        await EnsureLayoutProven();
        await _access.Require(identifier);
        uint? lifecycle = null;
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var current = await ReadFence(locations, identifier);
            var fence = current?.Fence ?? new EncryptionKeyErasure(0, [], true, Requests: []);
            lifecycle ??= fence.Lifecycle;
            if (fence.Lifecycle != lifecycle) throw new EncryptionKeyFenceChanged(identifier);
            if (current is not null && fence.Seal is not null) return fence;
            if (await _access.WriteFence(locations.ErasurePath, fence with { Version = checked(fence.Version + 1) }, current?.Version ?? 0) is not null)
            {
                return (await RequiredFence(locations, identifier)).Fence;
            }

            await _access.Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    async Task<(EncryptionKeyErasure Fence, int Version)> RequiredFence(VaultKeyLocations locations, EncryptionKeyIdentifier identifier) =>
        await ReadFence(locations, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);

    async Task<(EncryptionKeyErasure Fence, int Version)?> ReadFence(VaultKeyLocations locations, EncryptionKeyIdentifier identifier)
    {
        var current = await _access.ReadFence(locations.ErasurePath, identifier);
        if (current is not null || locations.LegacyErasurePath is not { } legacy) return current;

        // Legacy fence semantics are imported, but a new-path write always starts at native CAS zero.
        var earlier = await _access.ReadFence(legacy, identifier);
        return earlier is null ? null : (earlier.Value.Fence with { Seal = null }, 0);
    }

    async Task<List<EncryptionKeyAtRevision>> Present(VaultKeyLocations locations)
    {
        var present = new List<EncryptionKeyAtRevision>();
        foreach (var (revision, path) in await RevisionPaths(locations))
        {
            var row = await _access.Read(path);
            if (row is not null && Key(row.Value.Data) is { } key) present.Add(new(key, revision, Lifecycle(row.Value.Data)));
        }

        return present;
    }

    async Task<VaultKeyLocations> Resolve(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (locations.UnresolvedLegacyDirectory is null || await IsStoredAtNewLocation(locations) || !await HoldsEarlierMaterial(locations)) return locations;
        EncryptionKeyUpgradeMetrics.Refused();
        _logger.KeyNeedsUpgrade();
        throw new EncryptionKeyNeedsUpgrade();
    }

    async Task<bool> IsStoredAtNewLocation(VaultKeyLocations locations) =>
        (await ListRevisionsIn(locations.Directory)).Count != 0 || await _access.Read(locations.ErasurePath) is not null;

    async Task<bool> HoldsEarlierMaterial(VaultKeyLocations locations)
    {
        try
        {
            return (await ListRevisionsIn(locations.UnresolvedLegacyDirectory!)).Count != 0 || await _access.Read(locations.UnresolvedLegacyErasurePath!) is not null;
        }
        catch (VaultApiException)
        {
            return true;
        }
    }

    async Task EnsureLayoutProven()
    {
        if (_layoutProven) return;
        if ((await _layoutCheck.EnsureLayout()).LayoutUnusable) throw new EncryptionKeyLayoutUnusable();
        _layoutProven = true;
    }

    async Task<Dictionary<EncryptionKeyRevision, string>> ListRevisions(VaultKeyLocations locations)
    {
        var revisions = new Dictionary<EncryptionKeyRevision, string>();
        foreach (var (revision, path) in await RevisionPaths(locations)) revisions.TryAdd(revision, path);
        return revisions;
    }

    async Task<List<(EncryptionKeyRevision Revision, string Path)>> RevisionPaths(VaultKeyLocations locations)
    {
        var paths = (await ListRevisionsIn(locations.Directory)).ConvertAll(revision => (revision, locations.Path(revision)));
        if (locations.LegacyDirectory is not null)
        {
            paths.AddRange((await ListRevisionsIn(locations.LegacyDirectory)).Select(revision => (revision, locations.LegacyPath(revision)!)));
        }

        return paths;
    }

    async Task<List<EncryptionKeyRevision>> ListRevisionsIn(string directory)
    {
        try
        {
            var list = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(directory, mountPoint: _mountPoint);
            return list.Data.Keys.Where(key => !key.EndsWith('/') && uint.TryParse(key, out _)).Select(key => (EncryptionKeyRevision)uint.Parse(key, CultureInfo.InvariantCulture)).ToList();
        }
        catch (VaultApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }
}

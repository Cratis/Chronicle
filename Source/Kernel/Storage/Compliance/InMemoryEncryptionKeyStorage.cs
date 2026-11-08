// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for in-memory.
/// </summary>
[Singleton]
public partial class InMemoryEncryptionKeyStorage : IEncryptionKeyStorage, IEncryptionKeyFence
{
    readonly Dictionary<Key, EncryptionKeyAtRevision> _keys = [];
    readonly Dictionary<Scope, EncryptionKeyErasure> _erasures = [];
    readonly Dictionary<(EventStoreName EventStore, InternalSecretPurpose Purpose), EncryptionKey> _internalSecrets = [];
    readonly Lock _lock = new();

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave;

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureForRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        GetFenceFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var fence = EnsureFence(scope);
            var actualRevision = IsLatest(revision) ? GetNextRevision(scope) : revision!;
            Publish(scope, fence, new(key, actualRevision, fence.Lifecycle));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            return Task.FromResult(LatestPair(new(eventStore, eventStoreNamespace, identifier)));
        }
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            if (LatestPair(scope) is { } existing)
            {
                return Task.FromResult(existing);
            }

            var fence = EnsureFence(scope);
            var revision = fence.RevisionForNewKey(identifier, key);
            var staged = new EncryptionKeyAtRevision(key, revision, fence.Lifecycle);
            Publish(scope, fence, staged);
            return Task.FromResult(staged);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) is not null;

    /// <inheritdoc/>
    public Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var pair = IsLatest(revision) ? LatestPair(scope) : _keys.GetValueOrDefault(new(scope, revision!));
            return Task.FromResult(pair is not null && !ErasureFor(scope).Covers(pair) ? pair.Key : null);
        }
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetAtRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var pair = _keys.GetValueOrDefault(new(scope, revision));
            return Task.FromResult(pair is not null && !ErasureFor(scope).Covers(pair) ? pair : null);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            foreach (var entry in KeysFor(scope).Where(_ => IsLatest(revision) || _.Key.Revision == revision).ToArray())
            {
                _keys.Remove(entry.Key);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            var fence = ErasureFor(new(eventStore, eventStoreNamespace, identifier));
            var hasErasure = fence is not null && (fence.ErasedThrough.Value > 0 || fence.ErasedKeyFingerprints.Count > 0 || fence.ErasedLifecycle > 0);
            return Task.FromResult(hasErasure ? fence : null);
        }
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            return Task.FromResult(ErasureFor(new(eventStore, eventStoreNamespace, identifier)));
        }
    }

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        RecordErasureFor(eventStore, eventStoreNamespace, identifier, new(Guid.NewGuid().ToString()));

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var fence = EnsureFence(scope);
            var requests = fence.Requests ?? [];
            if (_archivedRequests.ContainsKey((scope, request.RequestId)) || requests.Any(_ => _.RequestId == request.RequestId))
            {
                return Task.CompletedTask;
            }

            if (request.Lifecycle != 0 && request.Lifecycle != fence.Lifecycle)
            {
                throw new EncryptionKeyFenceChanged(identifier);
            }

            if (requests.Count >= 1024)
            {
                throw new EncryptionKeyRequestLogFull(identifier);
            }

            var covered = EncryptionKeyErasure.Covering(fence, KeysFor(scope).Select(_ => (_.Key.Revision, _.Value.Key)));
            var recorded = new EncryptionKeyErasureRequest(request.RequestId, covered.ErasedThrough, fence.Lifecycle, covered.ErasedKeyFingerprints);
            _erasures[scope] = covered with { Requests = Array.AsReadOnly(requests.Append(recorded).ToArray()) };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var fence = RequiredFence(scope);
            if (_archivedRequests.ContainsKey((scope, requestId))) return Task.CompletedTask;
            var request = RequiredRequest(scope, fence, requestId);
            var boundary = Boundary(request);
            var removed = KeysFor(scope).Where(_ => boundary.Covers(_.Value)).ToArray();
            foreach (var entry in removed)
            {
                _keys.Remove(entry.Key);
            }

            var fingerprints = (request.Fingerprints ?? []).Concat(removed.Select(_ => _.Value.Key.Fingerprint)).Distinct(StringComparer.Ordinal).ToArray();
            if (fingerprints.Length != (request.Fingerprints?.Count ?? 0))
            {
                var updated = request with { Fingerprints = Array.AsReadOnly(fingerprints) };
                _erasures[scope] = fence with
                {
                    ErasedKeyFingerprints = Array.AsReadOnly(fence.ErasedKeyFingerprints.Concat(fingerprints).Distinct(StringComparer.Ordinal).ToArray()),
                    Requests = ReplaceRequest(fence, updated),
                    Version = checked(fence.Version + 1)
                };
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            if (_archivedRequests.ContainsKey((scope, requestId))) return Task.CompletedTask;
            var fence = RequiredFence(scope);
            var request = RequiredRequest(scope, fence, requestId);
            if (KeysFor(scope).Any(_ => Boundary(request).Covers(_.Value)))
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            if (!request.Confirmed)
            {
                _erasures[scope] = fence with
                {
                    Requests = ReplaceRequest(fence, request with { Confirmed = true }),
                    Version = checked(fence.Version + 1)
                };
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            if (ErasureFor(scope) is { NewKeyAllowed: false } fence)
            {
                return AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, fence.Version, fence.Lifecycle);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var fence = RequiredFence(scope);
            if (IsAuthorizationRetry(fence, observedVersion, observedLifecycle))
            {
                return Task.CompletedTask;
            }

            if (fence.Version != observedVersion || fence.Lifecycle != observedLifecycle || fence.NewKeyAllowed)
            {
                throw new EncryptionKeyFenceChanged(identifier);
            }

            _erasures[scope] = fence with
            {
                NewKeyAllowed = true,
                Version = checked(fence.Version + 1),
                Lifecycle = checked(fence.Lifecycle + 1)
            };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>Internal secrets live in their own dictionary, which no subject key member reads or writes.</remarks>
    public Task<EncryptionKey> GetOrAddInternalSecretFor(EventStoreName eventStore, InternalSecretPurpose purpose, EncryptionKey candidate)
    {
        lock (_lock)
        {
            return Task.FromResult(_internalSecrets.TryAdd((eventStore, purpose), candidate) ? candidate : _internalSecrets[(eventStore, purpose)]);
        }
    }

    static bool IsAuthorizationRetry(EncryptionKeyErasure fence, ulong observedVersion, uint observedLifecycle)
    {
        if (fence.Version <= observedVersion || fence.Lifecycle <= observedLifecycle)
        {
            return false;
        }

        return fence.NewKeyAllowed && fence.Version - observedVersion == 1 && fence.Lifecycle - observedLifecycle == 1;
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static EncryptionKeyErasure Boundary(EncryptionKeyErasureRequest request) =>
        new(request.Through!, request.Fingerprints ?? [], false, ErasedLifecycle: request.Lifecycle);

    static System.Collections.ObjectModel.ReadOnlyCollection<EncryptionKeyErasureRequest> ReplaceRequest(EncryptionKeyErasure fence, EncryptionKeyErasureRequest request) =>
        Array.AsReadOnly(fence.Requests!.Select(_ => _.RequestId == request.RequestId ? request : _).ToArray());

    static EncryptionKeyErasureRequest RequiredRequest(Scope scope, EncryptionKeyErasure fence, string requestId) =>
        fence.Requests?.FirstOrDefault(_ => _.RequestId == requestId) ?? throw new EncryptionKeyFenceChanged(scope.Identifier);

    void Publish(Scope scope, EncryptionKeyErasure observed, EncryptionKeyAtRevision staged)
    {
        // The lock makes staging, validation and publication indivisible. No staged row can be read, replaced or
        // swept between these steps; a failed validation has not changed any row, including an existing revision.
        var current = RequiredFence(scope);
        current.EnsureStillAllowed(scope.Identifier, staged);
        if (current.Version != observed.Version)
        {
            throw new EncryptionKeyFenceChanged(scope.Identifier);
        }

        _keys[new(scope, staged.Revision)] = staged;
    }

    EncryptionKeyErasure EnsureFence(Scope scope)
    {
        if (!_erasures.TryGetValue(scope, out var fence))
        {
            fence = new(0u, [], true);
            _erasures.Add(scope, fence);
        }

        return fence;
    }

    EncryptionKeyErasure? ErasureFor(Scope scope) => _erasures.GetValueOrDefault(scope);

    EncryptionKeyErasure RequiredFence(Scope scope) => ErasureFor(scope) ?? throw new EncryptionKeyFenceChanged(scope.Identifier);

    IEnumerable<KeyValuePair<Key, EncryptionKeyAtRevision>> KeysFor(Scope scope) => _keys.Where(_ => _.Key.Scope == scope);

    EncryptionKeyAtRevision? LatestPair(Scope scope) =>
        KeysFor(scope).Where(_ => !ErasureFor(scope).Covers(_.Value)).OrderByDescending(_ => _.Key.Revision.Value).Select(_ => _.Value).FirstOrDefault();

    EncryptionKeyRevision GetNextRevision(Scope scope) =>
        checked(Math.Max(KeysFor(scope).Select(_ => _.Key.Revision.Value).DefaultIfEmpty(0u).Max(), ErasureFor(scope)?.ErasedThrough.Value ?? 0u) + 1u);

    sealed record Scope(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier);

    sealed record Key(Scope Scope, EncryptionKeyRevision Revision);
}

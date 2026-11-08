// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents an <see cref="IEncryptionKeyStorage"/> that composes an ordered set of inner stores into one,
/// reading through them in order and healing the stores that are missing a key it found in a later one.
/// </summary>
/// <remarks>
/// <para>
/// This is the shape that turns moving encryption keys from one backend to another into an ordinary,
/// reversible cutover: a key that only exists in the store being left is still served, and is written into
/// the store being moved to the first time it is read. There is no flip window, no migration script, and no
/// verify pass — and because every write reaches every store, rolling back to the previous store loses nothing.
/// </para>
/// <para>
/// <b>Order is meaning.</b> The first store is the primary: it is read first and it is the only store
/// <see cref="GetOrAddFor"/> provisions on. A key found in a later store is healed into every earlier store;
/// the composite never writes backwards on a read, so a store you are moving away from is never grown by a read.
/// </para>
/// <para>
/// <b>An absence is only reported when every store agreed on it.</b> A store that could not be reached is
/// logged and skipped, but if no store produced a key and any of them failed, the failure is raised rather than
/// reported as "no key" — an untrue absence is indistinguishable from a completed right-to-erasure and would
/// silently blank every value it protects.
/// </para>
/// <para>
/// <b>Erasure must reach every store.</b> <see cref="DeleteFor"/> and <see cref="RecordErasureFor(EventStoreName, EventStoreNamespaceName, EncryptionKeyIdentifier)"/> attempt every
/// store even after one fails, and then report the failure: a key left behind in one store is healed back into the
/// others by the next read, so a partial erasure is not an erasure.
/// </para>
/// <para>
/// <b>One store's erasure fences all of them.</b> <see cref="GetErasureFor"/> answers with the strictest fence any
/// store holds, so provisioning is refused everywhere the moment one member records an erasure. A read that finds
/// key material any member recorded as destroyed returns nothing at all rather than healing the survivor around -
/// every member is asked, because which one kept a survivor and which one recorded the erasure is an accident of
/// ordering. Composition exists to move keys between backends; it must not become the path by which an erased key
/// moves back.
/// </para>
/// </remarks>
public class CompositeEncryptionKeyStorage : IEncryptionKeyStorage, IEvictEncryptionKeyCache
{
    readonly ILogger<CompositeEncryptionKeyStorage> _logger;
    readonly IEncryptionKeyStorage[] _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="inner">Ordered inner collection of <see cref="IEncryptionKeyStorage"/>. The first is the primary.</param>
    /// <exception cref="MissingInnerEncryptionKeyStorage">Thrown when no inner store is given.</exception>
    public CompositeEncryptionKeyStorage(params IEncryptionKeyStorage[] inner)
        : this(NullLogger<CompositeEncryptionKeyStorage>.Instance, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> for reporting partial failures.</param>
    /// <param name="inner">Ordered inner collection of <see cref="IEncryptionKeyStorage"/>. The first is the primary.</param>
    /// <exception cref="MissingInnerEncryptionKeyStorage">Thrown when no inner store is given.</exception>
    public CompositeEncryptionKeyStorage(ILogger<CompositeEncryptionKeyStorage> logger, params IEncryptionKeyStorage[] inner)
    {
        if (inner.Length == 0)
        {
            throw new MissingInnerEncryptionKeyStorage();
        }

        _logger = logger;
        _inner = inner;
        _logger.Composed(inner.Length);
    }

    /// <inheritdoc/>
    public IReadOnlyList<EncryptionKeyStorageCapability> Capabilities => _inner.SelectMany(_ => _.Capabilities).ToArray();

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => _inner.Aggregate(
        EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave,
        (guarantees, store) => guarantees & store.FenceGuarantees);

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fences = new List<EncryptionKeyErasure>();
        foreach (var store in _inner)
        {
            if (await store.GetFenceFor(eventStore, eventStoreNamespace, identifier) is { } fence)
            {
                fences.Add(fence);
            }
        }

        if (fences.Count == 0) return null;

        // This is a conservative read, not a shared CAS token. Authorization observes each authority separately.
        return Strictest(fences) with
        {
            Version = fences[0].Version,
            Lifecycle = fences.Min(_ => _.Lifecycle),
            ErasedLifecycle = fences.Max(_ => _.ErasedLifecycle),
            Requests = fences.SelectMany(_ => _.Requests ?? []).GroupBy(_ => _.RequestId, StringComparer.Ordinal)
                .Select(group => group.First() with { Confirmed = FenceGuarantees != EncryptionKeyFenceGuarantees.None && group.Count() == _inner.Length && group.All(_ => _.Confirmed) }).ToArray()
        };
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var requests = new List<EncryptionKeyErasureRequest>();
        foreach (var store in _inner)
        {
            if (await store.GetErasureRequestFor(eventStore, eventStoreNamespace, identifier, requestId) is { } request) requests.Add(request);
        }

        return requests.Count == 0 ? null : requests[0] with { Confirmed = requests.Count == _inner.Length && requests.TrueForAll(_ => _.Confirmed) };
    }

    /// <inheritdoc/>
    public async Task AuthorizeConfirmedSuccessorFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (FenceGuarantees != (EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave))
        {
            throw new EncryptionKeyLifecycleIncomplete(identifier, [new EncryptionKeyFenceUnavailable(identifier)]);
        }

        // Capture every member's own CAS version; independent authorities do not share a Version.
        var observations = new List<(IEncryptionKeyStorage Store, EncryptionKeyErasure Fence)>();
        foreach (var store in _inner)
        {
            var fence = await store.GetFenceFor(eventStore, eventStoreNamespace, identifier);
            if (fence?.Requests?.Any(_ => !_.Confirmed) == true) throw new EncryptionKeyFenceChanged(identifier);
            if (fence is { NewKeyAllowed: false }) observations.Add((store, fence));
        }

        var failures = new List<Exception>();
        foreach (var (store, fence) in observations)
        {
            try
            {
                await store.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, fence.Version, fence.Lifecycle);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32) =>
        EraseEvery(store => store.CompactErasureRequestsFor(eventStore, eventStoreNamespace, identifier, retain), identifier);

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request) =>
        EraseEvery(store => store.RecordErasureFor(eventStore, eventStoreNamespace, identifier, request), identifier);

    /// <inheritdoc/>
    public Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId) =>
        EraseEvery(store => store.DeleteThroughFor(eventStore, eventStoreNamespace, identifier, requestId), identifier);

    /// <inheritdoc/>
    public Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId) =>
        EraseEvery(store => store.ConfirmErasureFor(eventStore, eventStoreNamespace, identifier, requestId), identifier);

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        foreach (var store in _inner)
        {
            var fence = await store.GetFenceFor(eventStore, eventStoreNamespace, identifier);
            if (fence is null || fence.Version != observedVersion || fence.Lifecycle != observedLifecycle) throw new EncryptionKeyFenceChanged(identifier);
        }

        var failures = await ForEachStore(store => store.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, observedVersion, observedLifecycle));
        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        if (revision is null || revision == EncryptionKeyRevision.Latest)
        {
            return (await TryGetLatestFor(eventStore, eventStoreNamespace, identifier))?.Key;
        }

        List<Exception>? failures = null;
        var missing = new List<IEncryptionKeyStorage>();

        foreach (var store in _inner)
        {
            EncryptionKey? key;

            try
            {
                key = await store.TryGetFor(eventStore, eventStoreNamespace, identifier, revision);
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                LogFailure(error, () => _logger.ReadingFromInnerStoreFailed(identifier, error));
                continue;
            }

            if (key is null)
            {
                missing.Add(store);
                continue;
            }

            if (await AnyStoreDestroyed(eventStore, eventStoreNamespace, identifier, new(key, revision)))
            {
                _logger.ErasedKeySurvivedInAnotherStore(identifier);
                return null;
            }

            await Heal(missing, eventStore, eventStoreNamespace, identifier, key, revision);
            return key;
        }

        if (failures is not null)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyStorageUnavailable(identifier, failures);
        }

        return null;
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        ReadPairFor(eventStore, eventStoreNamespace, identifier, null);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetAtRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision) =>
        ReadPairFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        (await GetErasureForRelease(eventStore, eventStoreNamespace, identifier)).EnsureCanProvision(identifier, key);
        var provisioned = await _inner[0].GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key);
        var fence = await GetErasureForRelease(eventStore, eventStoreNamespace, identifier);
        if (fence.Covers(provisioned)) throw new EncryptionKeyErased(identifier, fence!);
        foreach (var store in _inner.Skip(1))
        {
            try
            {
                var mirrored = await store.GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, provisioned.Key);
                if (!mirrored.Key.Public.SequenceEqual(provisioned.Key.Public))
                {
                    _logger.InnerStoreHoldsDivergentKey(identifier);
                }
            }
            catch (Exception error)
            {
                LogFailure(error, () => _logger.MirroringToInnerStoreFailed(identifier, error));
            }
        }

        fence = await GetErasureForRelease(eventStore, eventStoreNamespace, identifier);
        if (fence.Covers(provisioned)) throw new EncryptionKeyErased(identifier, fence!);
        return provisioned;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        List<Exception>? failures = null;

        // This answers about presence, not about what a read hands over. Telling the truth about a key that is
        // physically there is the safe direction for both of its callers: a caller that would otherwise go and
        // copy the key stops, and one that wants the key gets it from TryGetFor, which is where the fence is
        // applied because that is where the material is known.
        foreach (var store in _inner)
        {
            try
            {
                if (await store.HasFor(eventStore, eventStoreNamespace, identifier, revision))
                {
                    return true;
                }
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                LogFailure(error, () => _logger.ReadingFromInnerStoreFailed(identifier, error));
            }
        }

        if (failures is not null)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyStorageUnavailable(identifier, failures);
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        var failures = await ForEachStore(store => store.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision));
        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeySaveIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var failures = await ForEachStore(store => store.DeleteFor(eventStore, eventStoreNamespace, identifier, revision));
        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        List<Exception>? failures = null;
        var erasures = new List<EncryptionKeyErasure>();

        foreach (var store in _inner)
        {
            try
            {
                if (await store.GetErasureFor(eventStore, eventStoreNamespace, identifier) is { } erasure)
                {
                    erasures.Add(erasure);
                }
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                LogFailure(error, () => _logger.ReadingFromInnerStoreFailed(identifier, error));
            }
        }

        // Nothing readable at all is the one case that cannot be answered: reporting "never erased" would let
        // provisioning mint over a fence nobody could see. A store that did answer is trusted even while another
        // is down, because refusing otherwise would stop every write of a protected value for the duration of a
        // secondary outage - and a fence that only the unreachable store holds means its erasure was already
        // reported incomplete, which is the signal to repeat it.
        if (failures is not null && failures.Count == _inner.Length)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyStorageUnavailable(identifier, failures);
        }

        return erasures.Count == 0 ? null : Strictest(erasures);
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var failures = await ForEachStore(store => store.RecordErasureFor(eventStore, eventStoreNamespace, identifier));
        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var failures = await ForEachStore(store => store.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier));
        if (failures.Count > 0)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Internal secrets live only in the primary, which is authoritative and which every silo reads, so they converge
    /// on one stored secret. When the primary cannot hold internal secrets the call fails rather than falling back to
    /// another store: a secret kept anywhere else would change with the order of the stores.
    /// </remarks>
    public Task<EncryptionKey> GetOrAddInternalSecretFor(EventStoreName eventStore, InternalSecretPurpose purpose, EncryptionKey candidate) =>
        _inner[0].GetOrAddInternalSecretFor(eventStore, purpose, candidate);

    /// <inheritdoc/>
    public void EvictFromCache(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        // Cluster-wide crypto-shred reaches the registered IEncryptionKeyStorage on every silo. The composite is
        // that registration, so it has to carry the eviction down to the caches its inner stores keep - without
        // this, a peer silo keeps serving a shredded key from a cache that nothing ever clears.
        foreach (var evictable in _inner.OfType<IEvictEncryptionKeyCache>())
        {
            evictable.EvictFromCache(eventStore, eventStoreNamespace, identifier);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureForRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        List<EncryptionKeyErasure> fences = [];
        List<Exception> failures = [];
        foreach (var store in _inner)
        {
            try
            {
                if (store.FenceGuarantees != (EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave))
                {
                    throw new EncryptionKeyFenceUnavailable(identifier);
                }

                if (await store.GetErasureForRelease(eventStore, eventStoreNamespace, identifier) is { } fence)
                {
                    fences.Add(fence);
                }
            }
            catch (EncryptionKeyIdentifierRefused error)
            {
                LogFailure(error, () => { });
                throw;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        if (failures.Count > 0) throw new EncryptionKeyFenceUnavailable(new AggregateException(failures));
        return fences.Count == 0 ? null : Strictest(fences) with
        {
            Version = fences.Max(_ => _.Version),
            ErasedLifecycle = fences.Max(_ => _.ErasedLifecycle)
        };
    }

    static EncryptionKeyErasure Strictest(List<EncryptionKeyErasure> erasures)
    {
        // The strictest fence wins: the highest floor, every fenced fingerprint any store knows about, and a new
        // lifecycle only where every store that recorded an erasure has authorized one. Merging towards the
        // lenient side would let the composition become a way to shop for a store that forgot.
        return new(
            erasures.Max(_ => _.ErasedThrough.Value),
            [.. erasures.SelectMany(_ => _.ErasedKeyFingerprints).Distinct(StringComparer.Ordinal)],
            erasures.TrueForAll(_ => _.NewKeyAllowed));
    }

    static Task<EncryptionKeyAtRevision?> ReadPairFrom(IEncryptionKeyStorage store, (EventStoreName Store, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier) address, EncryptionKeyRevision? revision) =>
        revision is null
            ? store.TryGetLatestFor(address.Store, address.Namespace, address.Identifier)
            : store.TryGetAtRevisionFor(address.Store, address.Namespace, address.Identifier, revision);

    async Task<EncryptionKeyAtRevision?> ReadPairFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision)
    {
        List<Exception>? failures = null;
        var missing = new List<IEncryptionKeyStorage>();
        foreach (var store in _inner)
        {
            EncryptionKeyAtRevision? pair;
            try
            {
                pair = await ReadPairFrom(store, (eventStore, eventStoreNamespace, identifier), revision);
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                LogFailure(error, () => _logger.ReadingFromInnerStoreFailed(identifier, error));
                continue;
            }

            if (pair is null)
            {
                missing.Add(store);
                continue;
            }

            if (await AnyStoreDestroyed(eventStore, eventStoreNamespace, identifier, pair))
            {
                _logger.ErasedKeySurvivedInAnotherStore(identifier);
                return null;
            }

            await Heal(missing, eventStore, eventStoreNamespace, identifier, pair.Key, pair.Revision);
            return pair;
        }

        if (failures is not null)
        {
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyStorageUnavailable(identifier, failures);
        }

        return null;
    }

    async Task EraseEvery(Func<IEncryptionKeyStorage, Task> operation, EncryptionKeyIdentifier identifier)
    {
        if (FenceGuarantees != (EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave))
        {
            // Still reach every member, but a legacy member can never confirm a composite erasure.
            var failures = await ForEachStore(operation);
            failures.Add(new EncryptionKeyFenceUnavailable(identifier));
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }

        var errors = await ForEachStore(operation);
        if (errors.Count > 0)
        {
            errors.ThrowIfUnderReview();
            throw new EncryptionKeyErasureIncomplete(identifier, errors);
        }
    }

    async Task<List<Exception>> ForEachStore(Func<IEncryptionKeyStorage, Task> operation)
    {
        List<Exception> failures = [];

        foreach (var store in _inner)
        {
            try
            {
                await operation(store);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        return failures;
    }

    async Task<bool> AnyStoreDestroyed(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyAtRevision pair) =>
        (await GetErasureForRelease(eventStore, eventStoreNamespace, identifier)).Covers(pair);

    async Task Heal(List<IEncryptionKeyStorage> stores, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision)
    {
        foreach (var store in stores)
        {
            try
            {
                await store.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
            }
            catch (Exception error)
            {
                // Healing is opportunistic - the caller already holds a usable key, and failing the read because
                // a store could not be written to would turn an available key into an outage.
                LogFailure(error, () => _logger.HealingInnerStoreFailed(identifier, error));
            }
        }
    }

    void LogFailure(Exception error, Action log)
    {
        if (error is EncryptionKeyIdentifierRefused)
        {
            // A store that refuses the identifier is never outvoted by one that has no opinion on it.
            _logger.InnerStoreRefusedIdentifier();
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        log();
    }
}

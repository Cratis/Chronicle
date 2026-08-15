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
/// <b>Erasure must reach every store.</b> <see cref="DeleteFor"/> and <see cref="RecordErasureFor"/> attempt every
/// store even after one fails, and then report the failure: a key left behind in one store is healed back into the
/// others by the next read, so a partial erasure is not an erasure.
/// </para>
/// <para>
/// <b>One store's erasure fences all of them.</b> <see cref="GetErasureFor"/> answers with the strictest fence any
/// store holds, and a read that finds a key in one store while another store has that identifier fenced returns
/// nothing at all rather than healing the survivor around. Composition exists to move keys between backends; it
/// must not become the path by which an erased key moves back.
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
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
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
                _logger.ReadingFromInnerStoreFailed(identifier, error);
                continue;
            }

            if (key is null)
            {
                missing.Add(store);
                continue;
            }

            if (await AnyFenced(missing, eventStore, eventStoreNamespace, identifier))
            {
                _logger.ErasedKeySurvivedInAnotherStore(identifier);
                return null;
            }

            await Heal(missing, eventStore, eventStoreNamespace, identifier, key, revision);
            return key;
        }

        if (failures is not null)
        {
            throw new EncryptionKeyStorageUnavailable(identifier, failures);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        // A fence recorded in any store refuses provisioning for all of them. Checking only the primary would let
        // a store that the erasure did not reach - or one that was added to the composition afterwards - become
        // the single place the subject gets a key again, which is the resurrection with extra steps.
        (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).EnsureCanProvision(identifier, key);

        // The primary is authoritative for provisioning. Provisioning on another store when it is unreachable
        // would let two silos mint different keys for the same subject, and every value encrypted under the
        // losing key would become permanently undecryptable - so a primary failure is raised, never worked around.
        var provisioned = await _inner[0].GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);

        foreach (var store in _inner.Skip(1))
        {
            try
            {
                // GetOrAddFor rather than SaveFor keeps the mirror idempotent - it never mints an additional
                // revision for a store that already holds a key for the identifier.
                var mirrored = await store.GetOrAddFor(eventStore, eventStoreNamespace, identifier, provisioned);
                if (!mirrored.Public.SequenceEqual(provisioned.Public))
                {
                    _logger.InnerStoreHoldsDivergentKey(identifier);
                }
            }
            catch (Exception error)
            {
                // The key is persisted on the primary and is usable. Failing the provisioning would stop every
                // write of a protected value while a secondary store is unavailable; the read path heals the
                // secondary the next time the key is asked for.
                _logger.MirroringToInnerStoreFailed(identifier, error);
            }
        }

        return provisioned;
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        List<Exception>? failures = null;
        var missing = new List<IEncryptionKeyStorage>();

        foreach (var store in _inner)
        {
            try
            {
                if (await store.HasFor(eventStore, eventStoreNamespace, identifier, revision))
                {
                    // Answering "yes" from a store while another store has the identifier fenced would tell the
                    // caller a key is available that TryGetFor refuses to hand over, and would send the
                    // cross-event-store copy looking for it.
                    if (await AnyFenced(missing, eventStore, eventStoreNamespace, identifier))
                    {
                        _logger.ErasedKeySurvivedInAnotherStore(identifier);
                        return false;
                    }

                    return true;
                }

                missing.Add(store);
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                _logger.ReadingFromInnerStoreFailed(identifier, error);
            }
        }

        if (failures is not null)
        {
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
            throw new EncryptionKeySaveIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var failures = await ForEachStore(store => store.DeleteFor(eventStore, eventStoreNamespace, identifier, revision));
        if (failures.Count > 0)
        {
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
                _logger.ReadingFromInnerStoreFailed(identifier, error);
            }
        }

        // Nothing readable at all is the one case that cannot be answered: reporting "never erased" would let
        // provisioning mint over a fence nobody could see. A store that did answer is trusted even while another
        // is down, because refusing otherwise would stop every write of a protected value for the duration of a
        // secondary outage - and a fence that only the unreachable store holds means its erasure was already
        // reported incomplete, which is the signal to repeat it.
        if (failures is not null && failures.Count == _inner.Length)
        {
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
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var failures = await ForEachStore(store => store.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier));
        if (failures.Count > 0)
        {
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }
    }

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

    async Task<bool> AnyFenced(List<IEncryptionKeyStorage> stores, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        // Only the stores that came up empty are asked, and only once one of the later stores produced a key - so
        // on the ordinary path, where the primary answers, this costs nothing. The case it catches is the
        // expensive one to be wrong about: an erasure that reached some members and not others, where healing
        // would put the key back and returning it would serve personal data meant to be unreadable.
        foreach (var store in stores)
        {
            try
            {
                if (await store.GetErasureFor(eventStore, eventStoreNamespace, identifier) is not null)
                {
                    return true;
                }
            }
            catch (Exception error)
            {
                // The store answered a read a moment ago, so failing here is an anomaly rather than an outage -
                // and guessing either way is worse than saying so. Guessing "not fenced" heals an erased key back;
                // guessing "fenced" blanks a live subject's data.
                _logger.ReadingFromInnerStoreFailed(identifier, error);
                throw new EncryptionKeyStorageUnavailable(identifier, [error]);
            }
        }

        return false;
    }

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
                _logger.HealingInnerStoreFailed(identifier, error);
            }
        }
    }
}

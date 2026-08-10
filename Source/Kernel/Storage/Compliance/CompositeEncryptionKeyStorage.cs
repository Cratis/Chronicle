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
/// <b>Erasure must reach every store.</b> <see cref="DeleteFor"/> attempts every store even after one fails, and
/// then reports the failure: a key left behind in one store is healed back into the others by the next read, so a
/// partial erasure is not an erasure.
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

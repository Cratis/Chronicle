// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> that works as a configurable cache in front of another <see cref="IEncryptionKeyStorage"/>.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="CacheEncryptionKeyStorage"/> class.
/// </para>
/// <para>
/// Alongside the cache of present keys, a short-lived negative cache remembers recently observed absences so a
/// repeatedly requested missing key does not hit the backing store on every lookup. The negative cache has a bounded
/// time-to-live because a key can be provisioned on another silo; once the entry expires the store is queried again.
/// Provisioning a key locally clears its negative entry immediately.
/// </para>
/// <para>
/// Every read releases the lock while it waits on the backing store, so a deletion, an eviction or a save can land in
/// that window. Each identifier therefore carries a local generation that advances on every mutation: a read captures
/// the generation before it releases the lock and only writes to the cache when the generation is unchanged once it
/// re-acquires it. Without that guard an in-flight read resurrects a key that was just erased, which for a
/// crypto-shredding key store means a completed right-to-erasure silently comes undone, and a read that started before
/// a rotation writes the superseded key back over the one that was just saved.
/// </para>
/// <para>
/// A deletion advances the generation twice - once before the backing store is asked to erase, and once after the
/// erase is durable. Only the second one closes the window: a read that misses the cache after the first advance and
/// reaches the backing store before the erase lands still holds the key, and still sees the generation it captured.
/// Cached keys have no time-to-live, so an entry written in that window would never be removed again.
/// </para>
/// <para>
/// Recording an erasure invalidates exactly like a deletion does, and for the same reason: the fence is written
/// before the key material is destroyed, so between those two steps the backing store still holds a key this cache
/// would otherwise keep serving. Erasure state itself is never cached - it is read on the provisioning path only,
/// which happens once per subject, and a stale "not erased" is the one answer that must never be given.
/// </para>
/// <para>
/// This makes the decorator self-sufficient for the store it wraps. It says nothing about caches on other silos -
/// those are reached by the cluster-wide eviction the erasure issues - and nothing about keys a peer store can heal
/// back into the wrapped store afterwards.
/// </para>
/// </remarks>
/// <param name="actualKeyStore">Actual <see cref="IEncryptionKeyStorage"/>.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/> used to expire negative cache entries; defaults to <see cref="TimeProvider.System"/>.</param>
/// <param name="negativeCacheTimeToLive">Optional duration an absence is remembered; defaults to <see cref="DefaultNegativeCacheTimeToLive"/>.</param>
public class CacheEncryptionKeyStorage(
    IEncryptionKeyStorage actualKeyStore,
    TimeProvider? timeProvider = null,
    TimeSpan? negativeCacheTimeToLive = null) : IEncryptionKeyStorage, IEvictEncryptionKeyCache
{
    /// <summary>
    /// Gets the default duration an absent key is remembered before the backing store is queried for it again.
    /// </summary>
    public static readonly TimeSpan DefaultNegativeCacheTimeToLive = TimeSpan.FromSeconds(5);

    readonly Dictionary<Key, EncryptionKey> _keys = [];
    readonly Dictionary<Key, DateTimeOffset> _absentKeys = [];
    readonly Dictionary<KeyScope, long> _generations = [];
    readonly Lock _lock = new();
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly TimeSpan _negativeCacheTimeToLive = negativeCacheTimeToLive ?? DefaultNegativeCacheTimeToLive;

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        Invalidate(scope, revision);

        try
        {
            await actualKeyStore.DeleteFor(eventStore, eventStoreNamespace, identifier, revision);
        }
        finally
        {
            // Invalidating again once the erase has landed is what actually closes the window - the first
            // invalidation only narrows it, because a read that misses the cache after it can still reach the backing
            // store while the key is there and then cache what it found. Invalidating in a finally covers a failed
            // erase too: a backing store that throws may still have removed some revisions, and the cache must not
            // keep serving what may already be gone.
            Invalidate(scope, revision);
        }
    }

    /// <inheritdoc/>
    public void EvictFromCache(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Invalidate(new KeyScope(eventStore, eventStoreNamespace, identifier), revision: null);

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        actualKeyStore.GetErasureFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        Invalidate(scope, revision: null);

        try
        {
            await actualKeyStore.RecordErasureFor(eventStore, eventStoreNamespace, identifier);
        }
        finally
        {
            // The fence lands before the key material is destroyed, so a read that missed the cache after the first
            // invalidation can still reach the backing store while the key is there and cache what it found.
            // Invalidating again once the fence is durable is what closes that window - the same shape the deletion
            // uses, and for the same reason.
            Invalidate(scope, revision: null);
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        await actualKeyStore.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier);

        // A remembered absence is what stops the next append reaching the store to provision the new lifecycle's
        // key, so the authorization has to clear it rather than wait out the negative cache's time-to-live.
        Invalidate(new KeyScope(eventStore, eventStoreNamespace, identifier), revision: null);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        var cacheKey = new Key(scope, EncryptionKeyRevision.Latest);
        long generation;

        lock (_lock)
        {
            if (_keys.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            generation = GenerationFor(scope);
        }

        var provisioned = await actualKeyStore.GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);
        lock (_lock)
        {
            if (generation == GenerationFor(scope))
            {
                _keys[cacheKey] = provisioned;
                _absentKeys.Remove(cacheKey);
            }
        }

        return provisioned;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        var cacheKey = new Key(scope, revision ?? EncryptionKeyRevision.Latest);
        long generation;

        lock (_lock)
        {
            if (_keys.TryGetValue(cacheKey, out var encryptionKey))
            {
                return encryptionKey;
            }

            if (IsRememberedAbsent(cacheKey))
            {
                return null;
            }

            generation = GenerationFor(scope);
        }

        var key = await actualKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier, revision);
        lock (_lock)
        {
            if (generation != GenerationFor(scope))
            {
                return key;
            }

            if (key is not null)
            {
                _keys[cacheKey] = key;
                _absentKeys.Remove(cacheKey);
            }
            else
            {
                RememberAbsent(cacheKey);
            }
        }

        return key;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        var cacheKey = new Key(scope, revision ?? EncryptionKeyRevision.Latest);
        long generation;

        lock (_lock)
        {
            if (_keys.ContainsKey(cacheKey))
            {
                return true;
            }

            if (IsRememberedAbsent(cacheKey))
            {
                return false;
            }

            generation = GenerationFor(scope);
        }

        var has = await actualKeyStore.HasFor(eventStore, eventStoreNamespace, identifier, revision);
        if (!has)
        {
            lock (_lock)
            {
                if (generation == GenerationFor(scope))
                {
                    RememberAbsent(cacheKey);
                }
            }
        }

        return has;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        var scope = new KeyScope(eventStore, eventStoreNamespace, identifier);
        lock (_lock)
        {
            // A save mutates the cache exactly like a deletion or an eviction does, so it has to move the generation
            // too - otherwise a read that captured the previous generation writes the key it found before the save
            // back over the one just written, and the cache serves the superseded key until something evicts it.
            // The save's own writes happen below under this same lock, so they are not fenced by their own advance.
            AdvanceGeneration(scope);

            var latest = new Key(scope, EncryptionKeyRevision.Latest);
            _keys.Remove(latest);

            if (revision is not null && revision != EncryptionKeyRevision.Latest)
            {
                var specific = new Key(scope, revision);
                _keys[specific] = key;
                _absentKeys.Remove(specific);
            }

            _keys[latest] = key;
            _absentKeys.Remove(latest);
        }

        await actualKeyStore.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static void RemoveAllFor<TValue>(Dictionary<Key, TValue> map, KeyScope scope)
    {
        foreach (var key in map.Keys.Where(_ => _.Scope == scope).ToList())
        {
            map.Remove(key);
        }
    }

    void Invalidate(KeyScope scope, EncryptionKeyRevision? revision)
    {
        lock (_lock)
        {
            AdvanceGeneration(scope);

            if (IsLatest(revision))
            {
                RemoveAllFor(_keys, scope);
                RemoveAllFor(_absentKeys, scope);
                return;
            }

            var specific = new Key(scope, revision!);
            var latest = new Key(scope, EncryptionKeyRevision.Latest);
            _keys.Remove(specific);
            _keys.Remove(latest);
            _absentKeys.Remove(specific);
            _absentKeys.Remove(latest);
        }
    }

    long GenerationFor(KeyScope scope) => _generations.GetValueOrDefault(scope);

    void AdvanceGeneration(KeyScope scope) => _generations[scope] = GenerationFor(scope) + 1;

    bool IsRememberedAbsent(Key cacheKey)
    {
        if (_absentKeys.TryGetValue(cacheKey, out var expiresAt))
        {
            if (_timeProvider.GetUtcNow() < expiresAt)
            {
                return true;
            }

            _absentKeys.Remove(cacheKey);
        }

        return false;
    }

    void RememberAbsent(Key cacheKey) => _absentKeys[cacheKey] = _timeProvider.GetUtcNow() + _negativeCacheTimeToLive;

    sealed record KeyScope(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier);

    sealed record Key(KeyScope Scope, EncryptionKeyRevision Revision);
}

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
    readonly Lock _lock = new();
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly TimeSpan _negativeCacheTimeToLive = negativeCacheTimeToLive ?? DefaultNegativeCacheTimeToLive;

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            if (IsLatest(revision))
            {
                RemoveAllFor(_keys, eventStore, eventStoreNamespace, identifier);
                RemoveAllFor(_absentKeys, eventStore, eventStoreNamespace, identifier);
            }
            else
            {
                var specific = new Key(eventStore, eventStoreNamespace, identifier, revision!);
                var latest = new Key(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest);
                _keys.Remove(specific);
                _keys.Remove(latest);
                _absentKeys.Remove(specific);
                _absentKeys.Remove(latest);
            }
        }

        await actualKeyStore.DeleteFor(eventStore, eventStoreNamespace, identifier, revision);
    }

    /// <inheritdoc/>
    public void EvictFromCache(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        lock (_lock)
        {
            RemoveAllFor(_keys, eventStore, eventStoreNamespace, identifier);
            RemoveAllFor(_absentKeys, eventStore, eventStoreNamespace, identifier);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var cacheKey = new Key(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest);
        lock (_lock)
        {
            if (_keys.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }

        var provisioned = await actualKeyStore.GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);
        lock (_lock)
        {
            _keys[cacheKey] = provisioned;
            _absentKeys.Remove(cacheKey);
        }

        return provisioned;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var cacheRevision = revision ?? EncryptionKeyRevision.Latest;
        var cacheKey = new Key(eventStore, eventStoreNamespace, identifier, cacheRevision);

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
        }

        var key = await actualKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier, revision);
        lock (_lock)
        {
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
        var cacheRevision = revision ?? EncryptionKeyRevision.Latest;
        var cacheKey = new Key(eventStore, eventStoreNamespace, identifier, cacheRevision);

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
        }

        var has = await actualKeyStore.HasFor(eventStore, eventStoreNamespace, identifier, revision);
        if (!has)
        {
            lock (_lock)
            {
                RememberAbsent(cacheKey);
            }
        }

        return has;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            var latest = new Key(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest);
            _keys.Remove(latest);

            if (revision is not null && revision != EncryptionKeyRevision.Latest)
            {
                var specific = new Key(eventStore, eventStoreNamespace, identifier, revision);
                _keys[specific] = key;
                _absentKeys.Remove(specific);
            }

            _keys[latest] = key;
            _absentKeys.Remove(latest);
        }

        await actualKeyStore.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static void RemoveAllFor<TValue>(Dictionary<Key, TValue> map, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var keysToRemove = map.Keys
            .Where(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier)
            .ToList();
        foreach (var key in keysToRemove)
        {
            map.Remove(key);
        }
    }

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

    sealed record Key(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier, EncryptionKeyRevision Revision);
}

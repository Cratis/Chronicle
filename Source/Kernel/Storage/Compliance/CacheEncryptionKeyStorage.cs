// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> that works as a configurable cache in front of another <see cref="IEncryptionKeyStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CacheEncryptionKeyStorage"/> class.
/// </remarks>
/// <param name="actualKeyStore">Actual <see cref="IEncryptionKeyStorage"/>.</param>
public class CacheEncryptionKeyStorage(IEncryptionKeyStorage actualKeyStore) : IEncryptionKeyStorage
{
    readonly Dictionary<Key, EncryptionKey> _keys = [];

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            var keysToRemove = _keys.Keys
                .Where(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier)
                .ToList();
            foreach (var key in keysToRemove)
            {
                _keys.Remove(key);
            }
        }
        else
        {
            _keys.Remove(new(eventStore, eventStoreNamespace, identifier, revision!));
            _keys.Remove(new(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest));
        }

        await actualKeyStore.DeleteFor(eventStore, eventStoreNamespace, identifier, revision);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var cacheRevision = revision ?? EncryptionKeyRevision.Latest;
        var cacheKey = new Key(eventStore, eventStoreNamespace, identifier, cacheRevision);

        if (_keys.TryGetValue(cacheKey, out var encryptionKey))
        {
            return encryptionKey;
        }

        return _keys[cacheKey] = await actualKeyStore.GetFor(eventStore, eventStoreNamespace, identifier, revision);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var cacheRevision = revision ?? EncryptionKeyRevision.Latest;

        if (_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier, cacheRevision)))
        {
            return true;
        }

        return await actualKeyStore.HasFor(eventStore, eventStoreNamespace, identifier, revision);
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        _keys.Remove(new(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest));

        if (revision is not null && revision != EncryptionKeyRevision.Latest)
        {
            _keys[new(eventStore, eventStoreNamespace, identifier, revision)] = key;
        }

        _keys[new(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Latest)] = key;
        await actualKeyStore.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    sealed record Key(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier, EncryptionKeyRevision Revision);
}

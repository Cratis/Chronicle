// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> that works as a configurable cache in front of another <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public class CacheEncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly IEncryptionKeyStorage _actualKeyStore;
    readonly Dictionary<Key, EncryptionKey> _keys = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="actualKeyStore">Actual <see cref="IEncryptionKeyStorage"/>.</param>
    public CacheEncryptionKeyStorage(IEncryptionKeyStorage actualKeyStore)
    {
        _actualKeyStore = actualKeyStore;
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        _keys.Remove(new(eventStore, eventStoreNamespace, identifier));
        await _actualKeyStore.DeleteFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var key = new Key(eventStore, eventStoreNamespace, identifier);
        if (_keys.TryGetValue(key, out var encryptionKey)) return encryptionKey;

        return _keys[key] = await _actualKeyStore.GetFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier)))
        {
            return true;
        }

        return await _actualKeyStore.HasFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        _keys[new(eventStore, eventStoreNamespace, identifier)] = key;
        await _actualKeyStore.SaveFor(eventStore, eventStoreNamespace, identifier, key);
    }

    record Key(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}

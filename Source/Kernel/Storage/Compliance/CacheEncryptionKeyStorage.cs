// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;

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
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        _keys.Remove(new(eventStore, eventStoreNamespace, identifier));
        await actualKeyStore.DeleteFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var key = new Key(eventStore, eventStoreNamespace, identifier);
        if (_keys.TryGetValue(key, out var encryptionKey)) return encryptionKey;

        return _keys[key] = await actualKeyStore.GetFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier)))
        {
            return true;
        }

        return await actualKeyStore.HasFor(eventStore, eventStoreNamespace, identifier);
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        _keys[new(eventStore, eventStoreNamespace, identifier)] = key;
        await actualKeyStore.SaveFor(eventStore, eventStoreNamespace, identifier, key);
    }

    record Key(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}

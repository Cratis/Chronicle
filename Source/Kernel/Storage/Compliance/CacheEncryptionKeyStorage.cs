// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> that works as a configurable cache in front of another <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public class CacheEncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly IEncryptionKeyStorage _actualKeyStore;
    readonly Dictionary<EncryptionKeyIdentifier, EncryptionKey> _keys = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="actualKeyStore">Actual <see cref="IEncryptionKeyStorage"/>.</param>
    public CacheEncryptionKeyStorage(IEncryptionKeyStorage actualKeyStore)
    {
        _actualKeyStore = actualKeyStore;
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EncryptionKeyIdentifier identifier)
    {
        _keys.Remove(identifier);
        await _actualKeyStore.DeleteFor(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier)
    {
        if (_keys.TryGetValue(identifier, out var key)) return key;

        return _keys[identifier] = await _actualKeyStore.GetFor(identifier);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EncryptionKeyIdentifier identifier)
    {
        if (_keys.ContainsKey(identifier))
        {
            return true;
        }

        return await _actualKeyStore.HasFor(identifier);
    }

    /// <inheritdoc/>
    public async Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        _keys[identifier] = key;
        await _actualKeyStore.SaveFor(identifier, key);
    }
}

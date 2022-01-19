// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Represents an implementation of <see cref="IEncryptionKeyStore"/> that works as a configurable cache in front of another <see cref="IEncryptionKeyStore"/>.
    /// </summary>
    public class CacheEncryptionKeyStore : IEncryptionKeyStore
    {
        readonly IEncryptionKeyStore _actualKeyStore;
        readonly Dictionary<EncryptionKeyIdentifier, EncryptionKey> _keys = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEncryptionKeyStore"/> class.
        /// </summary>
        /// <param name="actualKeyStore">Actual <see cref="IEncryptionKeyStore"/>.</param>
        public CacheEncryptionKeyStore(IEncryptionKeyStore actualKeyStore)
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
            if (_keys.ContainsKey(identifier))
            {
                return _keys[identifier];
            }

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
}

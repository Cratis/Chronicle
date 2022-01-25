// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Compliance.InMemory
{
    /// <summary>
    /// Represents an implementation of <see cref="IEncryptionKeyStore"/> for in-memory.
    /// </summary>
    [Singleton]
    public class InMemoryEncryptionKeyStore : IEncryptionKeyStore
    {
        readonly Dictionary<EncryptionKeyIdentifier, EncryptionKey> _keys = new();

        /// <inheritdoc/>
        public Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
        {
            _keys[identifier] = key;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> HasFor(EncryptionKeyIdentifier identifier) => Task.FromResult(_keys.ContainsKey(identifier));

        /// <inheritdoc/>
        public Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier) => Task.FromResult(_keys[identifier]);

        /// <inheritdoc/>
        public Task DeleteFor(EncryptionKeyIdentifier identifier)
        {
            _keys.Remove(identifier);
            return Task.CompletedTask;
        }
    }
}

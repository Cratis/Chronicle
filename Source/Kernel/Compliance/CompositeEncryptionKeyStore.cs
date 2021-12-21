// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance
{
    /// <summary>
    /// Represents a composite <see cref="IEncryptionKeyStore"/>.
    /// </summary>
    public class CompositeEncryptionKeyStore : IEncryptionKeyStore
    {
        readonly IEncryptionKeyStore[] _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeEncryptionKeyStore"/>.
        /// </summary>
        /// <param name="inner"></param>
        public CompositeEncryptionKeyStore(params IEncryptionKeyStore[] inner)
        {
            _inner = inner;
        }

        /// <inheritdoc/>
        public async Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier)
        {
            foreach (var innerStore in _inner)
            {
                if (await innerStore.HasFor(identifier))
                {
                    return await innerStore.GetFor(identifier);
                }
            }

            throw new MissingEncryptionKey(identifier);
        }

        /// <inheritdoc/>
        public async Task<bool> HasFor(EncryptionKeyIdentifier identifier)
        {
            foreach (var innerStore in _inner)
            {
                if (await innerStore.HasFor(identifier))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
        {
            foreach (var innerStore in _inner)
            {
                await innerStore.SaveFor(identifier, key);
            }
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance
{
    /// <summary>
    /// Defines a store for holding <see cref="EncryptionKey">encryption keys</see>.
    /// </summary>
    public interface IEncryptionKeyStore
    {
        /// <summary>
        /// Save an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
        /// </summary>
        /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to save for.</param>
        /// <param name="key">The <see cref="EncryptionKey"/>.</param>
        /// <returns>Async task.</returns>
        Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key);

        /// <summary>
        /// Check if there is an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
        /// </summary>
        /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to check for.</param>
        /// <returns>True if there is, false if not.</returns>
        Task<bool> HasFor(EncryptionKeyIdentifier identifier);

        /// <summary>
        /// Get an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
        /// </summary>
        /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to get for.</param>
        /// <returns>The <see cref="EncryptionKey"/>.</returns>
        Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier);
    }
}

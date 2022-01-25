// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a system for working with encryption.
    /// </summary>
    public interface IEncryption
    {
        /// <summary>
        /// Generate an <see cref="EncryptionKey"/>.
        /// </summary>
        /// <returns>A new <see cref="EncryptionKey"/>.</returns>
        EncryptionKey GenerateKey();

        /// <summary>
        /// Encrypt bytes with a key.
        /// </summary>
        /// <param name="bytes">Bytes to encrypt.</param>
        /// <param name="key">Key to use.</param>
        /// <returns>Encrypted bytes.</returns>
        byte[] Encrypt(byte[] bytes, EncryptionKey key);

        /// <summary>
        /// Decrypt bytes with a key.
        /// </summary>
        /// <param name="bytes">Bytes to decrypt.</param>
        /// <param name="key">Key to use.</param>
        /// <returns>Decrypted bytes.</returns>
        byte[] Decrypt(byte[] bytes, EncryptionKey key);
    }
}

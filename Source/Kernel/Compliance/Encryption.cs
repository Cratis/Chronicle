// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Compliance.InMemory
{
    /// <summary>
    /// Represents an implementation of <see cref="IEncryption"/>.
    /// </summary>
    public class Encryption : IEncryption
    {
        /// <inheritdoc/>
        public EncryptionKey GenerateKey()
        {
            var aes = CreateAes();
            aes.GenerateKey();
            return new(aes.Key);
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes, EncryptionKey key)
        {
            var aes = CreateAes();
            aes.GenerateIV();
            aes.Key = key.Value;
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return aes.IV.Concat(encrypted).ToArray();
        }

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes, EncryptionKey key)
        {
            var aes = CreateAes();
            aes.IV = bytes[..16];
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(bytes, aes.IV.Length, bytes.Length - aes.IV.Length);
        }

        Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}

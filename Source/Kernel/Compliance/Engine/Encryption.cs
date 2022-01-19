// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Represents an implementation of <see cref="IEncryption"/>.
    /// </summary>
    public class Encryption : IEncryption
    {
        const int KeySize = 2048;

        /// <inheritdoc/>
        public EncryptionKey GenerateKey()
        {
            var rsa = RSA.Create(KeySize);
            var privateKey = rsa.ExportRSAPrivateKey();
            var publicKey = rsa.ExportRSAPublicKey();
            return new(publicKey, privateKey);
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes, EncryptionKey key)
        {
            using var provider = new RSACryptoServiceProvider(KeySize);
            provider.ImportRSAPublicKey(key.Public, out _);
            return provider.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
        }

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes, EncryptionKey key)
        {
            using var provider = new RSACryptoServiceProvider(KeySize);
            provider.ImportRSAPrivateKey(key.Private, out _);
            return provider.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
        }
    }
}

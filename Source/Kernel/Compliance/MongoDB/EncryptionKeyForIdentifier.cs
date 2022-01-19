// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819 // Allow arrays for properties

namespace Aksio.Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents the stored version of an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="Identifier"><see cref="EncryptionKeyIdentifier"/> it is for.</param>
    /// <param name="PublicKey">The public part of the key.</param>
    /// <param name="PrivateKey">The private part of the key.</param>
    public record EncryptionKeyForIdentifier(EncryptionKeyIdentifier Identifier, byte[] PublicKey, byte[] PrivateKey);
}

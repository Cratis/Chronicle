// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

#pragma warning disable CA1819

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents a key used in encryption.
/// </summary>
/// <param name="Public">The public part of the key.</param>
/// <param name="Private">The private part of the key.</param>
public record EncryptionKey(byte[] Public, byte[] Private)
{
    /// <summary>
    /// Gets the fingerprint of the key - the SHA-256 of its public part, as lowercase hex.
    /// </summary>
    /// <remarks>
    /// This names key material without holding any, which is what lets an <see cref="EncryptionKeyErasure"/>
    /// remember the key it destroyed and refuse that exact material if something tries to store it again. Only the
    /// public part is hashed: it is the half a copy carries alongside the private one, and hashing the private key
    /// would put a derivative of secret material into a record that deliberately outlives it.
    /// </remarks>
    public string Fingerprint => Convert.ToHexStringLower(SHA256.HashData(Public));
}

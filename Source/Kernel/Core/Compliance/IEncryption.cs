// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

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
    /// <param name="revision">The concrete revision of the key used.</param>
    /// <returns>Encrypted bytes.</returns>
    byte[] Encrypt(byte[] bytes, EncryptionKey key, EncryptionKeyRevision revision);

    /// <summary>Reads the key revision from a v2 envelope without decrypting it.</summary>
    /// <param name="bytes">The encrypted bytes.</param>
    /// <param name="revision">The recorded revision, when present.</param>
    /// <returns>Whether the value carries a concrete key revision.</returns>
    bool TryGetKeyRevision(byte[] bytes, out EncryptionKeyRevision revision);

    /// <summary>
    /// Decrypt bytes with a key.
    /// </summary>
    /// <param name="bytes">Bytes to decrypt.</param>
    /// <param name="key">Key to use.</param>
    /// <returns>Decrypted bytes.</returns>
    byte[] Decrypt(byte[] bytes, EncryptionKey key);

    /// <summary>
    /// Check whether bytes carry the shape this encryption produces.
    /// </summary>
    /// <param name="bytes">Bytes to check.</param>
    /// <returns>True if the bytes could have been produced by <see cref="Encrypt"/>, false otherwise.</returns>
    /// <remarks>
    /// This tells apart a value that was encrypted from one that never was, so that releasing a value which
    /// was never protected can be a no-op instead of an error. It is a structural check on the format only —
    /// it says nothing about which subject's key the value belongs to, and a value it accepts can still fail
    /// to decrypt.
    /// </remarks>
    bool IsEncrypted(byte[] bytes);
}

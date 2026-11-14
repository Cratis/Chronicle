// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the protection of the private keys hosted federation holds, the authority key and the key of an enrolled identity.
/// </summary>
/// <remarks>
/// A key is wrapped with a key-encryption key the operator supplies outside the database, bound to what the key is for, so that
/// someone who can read the databases and the event log still holds nothing they can sign with.
/// </remarks>
public interface IFederationKeyProtection
{
    /// <summary>
    /// Wraps a private key.
    /// </summary>
    /// <param name="plaintext">The key to wrap, PKCS#8.</param>
    /// <param name="purpose">What the key is for, for example <c language="text">authority</c> or <c language="text">identity</c>.</param>
    /// <param name="identity">The identity the key belongs to.</param>
    /// <param name="serial">The serial number of the certificate the key belongs to.</param>
    /// <returns>The wrapped key.</returns>
    /// <exception cref="FederationKeyEncryptionKeyUnavailable">Thrown when there is no key-encryption key to wrap with.</exception>
    Task<byte[]> Protect(byte[] plaintext, string purpose, string identity, string serial);

    /// <summary>
    /// Unwraps a private key wrapped by <see cref="Protect"/>.
    /// </summary>
    /// <param name="wrapped">The wrapped key.</param>
    /// <param name="purpose">What the key is for.</param>
    /// <param name="identity">The identity the key belongs to.</param>
    /// <param name="serial">The serial number of the certificate the key belongs to.</param>
    /// <returns>The key, PKCS#8. The caller must zero it after use.</returns>
    /// <exception cref="FederationKeyEncryptionKeyUnavailable">Thrown when there is no key-encryption key to unwrap with.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when the key was wrapped for something else, or under another key-encryption key.</exception>
    Task<byte[]> Unprotect(byte[] wrapped, string purpose, string identity, string serial);
}

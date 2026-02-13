// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Defines a system for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryption
{
    /// <summary>
    /// Encrypts a plain text string.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt.</param>
    /// <returns>The decrypted plain text.</returns>
    string Decrypt(string encryptedText);
}

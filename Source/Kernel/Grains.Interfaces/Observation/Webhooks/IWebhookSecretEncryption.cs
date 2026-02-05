// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a service for encrypting and decrypting webhook secrets.
/// </summary>
public interface IWebhookSecretEncryption
{
    /// <summary>
    /// Encrypts a secret value.
    /// </summary>
    /// <param name="plainText">The plain text secret to encrypt.</param>
    /// <returns>The encrypted secret as a base64 string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted secret value.
    /// </summary>
    /// <param name="encryptedText">The encrypted secret as a base64 string.</param>
    /// <returns>The decrypted plain text secret.</returns>
    string Decrypt(string encryptedText);
}

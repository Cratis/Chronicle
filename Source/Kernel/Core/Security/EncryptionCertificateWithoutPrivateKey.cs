// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when a certificate in the encryption-certificate ring carries no private key.
/// </summary>
/// <param name="keyId">The key id of the certificate.</param>
/// <param name="certificatePath">The path the certificate was loaded from.</param>
public class EncryptionCertificateWithoutPrivateKey(string keyId, string certificatePath)
    : Exception(
        $"The encryption certificate with key id '{keyId}', loaded from '{certificatePath}', carries no private key. " +
        "Every certificate in the ring has to decrypt, so export the PKCS#12 file with its private key included.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the encryption certificate configuration for Data Protection.
/// </summary>
public class EncryptionCertificate
{
    /// <summary>
    /// Gets the path to the certificate file (.pfx) for encrypting Data Protection keys.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    /// Gets a value indicating whether a certificate is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(CertificatePath);
}

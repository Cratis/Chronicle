// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a certificate that has been rotated out of the active position and is kept for decryption only.
/// </summary>
/// <remarks>
/// A previous certificate is never used to protect anything new. It stays in the ring so values written
/// while it was active remain readable, and it is removed once nothing depends on it any longer.
/// </remarks>
public class PreviousEncryptionCertificate
{
    /// <summary>
    /// Gets the path to the certificate file (.pfx) that was previously active.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }
}

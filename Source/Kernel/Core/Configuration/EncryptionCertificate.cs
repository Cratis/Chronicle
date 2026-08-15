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
    /// <remarks>
    /// This is the <b>active</b> certificate — the one everything written from now on is protected with.
    /// </remarks>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    /// Gets the certificates that were previously active and are kept loaded for decryption only, most recent first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes a rotation possible without downtime: the new certificate goes into
    /// <see cref="CertificatePath"/> and the one it replaces moves in here, so values protected by it stay
    /// readable while everything new is protected by the active one.
    /// </para>
    /// <para>
    /// A previous certificate is removed once nothing depends on it any longer — which
    /// <c>GET /diagnostics/encryption-certificates</c> reports. Removing it while something still does makes
    /// that data unreadable, so keep the file in the backup set for as long as the oldest restorable backup.
    /// </para>
    /// </remarks>
    public IEnumerable<PreviousEncryptionCertificate> Previous { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether a certificate is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(CertificatePath);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the state of the encryption-certificate ring as an operator needs to see it.
/// </summary>
/// <param name="IsConfigured">Whether an encryption certificate is configured at all.</param>
/// <param name="ActiveKeyId">The key id of the active certificate, or an empty string when none is configured.</param>
/// <param name="Certificates">Every certificate in the ring, active first.</param>
public record EncryptionCertificateRingStatus(
    bool IsConfigured,
    string ActiveKeyId,
    IEnumerable<EncryptionCertificateStatus> Certificates)
{
    /// <summary>
    /// Gets the ring state of a deployment with no encryption certificate configured.
    /// </summary>
    public static EncryptionCertificateRingStatus NotConfigured { get; } = new(false, string.Empty, []);

    /// <summary>
    /// Gets a value indicating whether the ring holds certificates beyond the active one.
    /// </summary>
    public bool IsRotating => Certificates.Any(_ => _.Role == EncryptionCertificateRole.Previous);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents everything an operator needs to decide whether a rotation is finished.
/// </summary>
/// <param name="Ring">The certificates this node is running with.</param>
/// <param name="DataProtectionKeys">What the stored Data Protection keys depend on, one entry per certificate.</param>
/// <param name="KeysNotProtectedByCertificate">The number of stored Data Protection keys no certificate can be attributed to.</param>
/// <remarks>
/// <para>
/// The Data Protection numbers are facts read out of storage: each stored key carries the certificate it is
/// encrypted to. Chronicle's own value encryption is not counted here — nothing enumerates webhook
/// credentials — so a previous certificate reported as carrying no Data Protection keys may still be needed
/// by one. The log records that separately, the first time such a value is read.
/// </para>
/// <para>
/// A non-zero <paramref name="KeysNotProtectedByCertificate"/> means keys are stored without certificate
/// protection, which is what a deployment running with no encryption certificate produces.
/// </para>
/// </remarks>
public record EncryptionCertificateRotationReport(
    EncryptionCertificateRingStatus Ring,
    IEnumerable<DataProtectionKeyDependency> DataProtectionKeys,
    int KeysNotProtectedByCertificate)
{
    /// <summary>
    /// Gets a value indicating whether stored Data Protection keys still depend on a previous certificate.
    /// </summary>
    public bool PreviousCertificatesInUse =>
        DataProtectionKeys.Any(_ => _.Role == EncryptionCertificateRole.Previous && _.KeyCount > 0);

    /// <summary>
    /// Gets a value indicating whether stored Data Protection keys depend on a certificate that has left the ring.
    /// </summary>
    /// <remarks>
    /// Those keys are already unreadable. It is what a rotation carried out in the wrong order looks like,
    /// and what a restore into a ring that has moved on looks like.
    /// </remarks>
    public bool RetiredCertificatesInUse =>
        DataProtectionKeys.Any(_ => _.Role == EncryptionCertificateRole.Retired && _.KeyCount > 0);

    /// <summary>
    /// Gets a value indicating whether the previous certificates can be removed from the ring.
    /// </summary>
    public bool CanRetirePreviousCertificates => Ring.IsRotating && !PreviousCertificatesInUse;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents why a hosted federation peer was not admitted. The reason is for logs and specifications only: the peer is
/// always given the same generic answer, so nothing about what exists can be learned.
/// </summary>
/// <remarks>
/// The values are explicit and never renumbered.
/// </remarks>
public enum FederationAdmissionRefusal
{
    /// <summary>
    /// The peer was admitted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hosted federation is not enabled on this kernel.
    /// </summary>
    NotEnabled = 1,

    /// <summary>
    /// No usable client certificate was presented.
    /// </summary>
    NoCertificate = 2,

    /// <summary>
    /// This kernel has no federation authority, so it cannot have issued the certificate.
    /// </summary>
    NoAuthority = 3,

    /// <summary>
    /// The certificate does not chain to the federation authority, or is outside its validity period.
    /// </summary>
    NotChained = 4,

    /// <summary>
    /// The identity in the certificate does not have the expected form.
    /// </summary>
    WrongIdentity = 5,

    /// <summary>
    /// The identity is in another trust domain.
    /// </summary>
    WrongTrustDomain = 6,

    /// <summary>
    /// The serial number is not in the registry of certificates this kernel issued, however well it is signed.
    /// </summary>
    NotIssued = 7,

    /// <summary>
    /// The certificate was issued by this kernel and later revoked.
    /// </summary>
    CertificateRevoked = 8,

    /// <summary>
    /// The identity in the certificate is not the one the registry recorded for its serial number.
    /// </summary>
    SubjectMismatch = 9,

    /// <summary>
    /// The subject has no policy.
    /// </summary>
    NoPolicy = 10,

    /// <summary>
    /// The policy of the subject is suspended.
    /// </summary>
    Suspended = 11,

    /// <summary>
    /// The policy of the subject is revoked.
    /// </summary>
    Revoked = 12,

    /// <summary>
    /// The enrollment the certificate descends from was revoked, whether or not the certificate itself was.
    /// </summary>
    EnrollmentRevoked = 13,

    /// <summary>
    /// The certificate is not the current one of its subject: a newer certificate was issued, so only that one is admitted.
    /// </summary>
    Superseded = 14
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The state of a kernel as a peer of a hosted federation authority. Neither key material nor the enrollment code is part of it.
/// </summary>
/// <param name="Enrolled">Whether the kernel holds a certificate from an authority.</param>
/// <param name="Identity">The identity in the certificate. Empty when not enrolled.</param>
/// <param name="Serial">The serial number of the certificate. Empty when not enrolled.</param>
/// <param name="NotAfter">When the certificate expires, or null when not enrolled.</param>
/// <param name="RenewAfter">When the certificate becomes due for renewal, or null when not enrolled.</param>
/// <param name="Endpoint">The public endpoint of the authority. Empty when not enrolled.</param>
/// <param name="AuthorityFingerprint">The SHA-256 fingerprint of the authority root, as lowercase hex. Empty when not enrolled.</param>
/// <param name="Expired">Whether the certificate has expired, so the kernel is locked out until it is enrolled again.</param>
public record FederationEnrollmentStatus(
    bool Enrolled,
    string Identity,
    string Serial,
    DateTimeOffset? NotAfter,
    DateTimeOffset? RenewAfter,
    string Endpoint,
    string AuthorityFingerprint,
    bool Expired);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The authority issued a certificate to a peer. The event source id is the serial number. This is the registry of issued certificates.
/// </summary>
/// <param name="EnrollmentId">The enrollment the certificate descends from, including through renewals.</param>
/// <param name="Subject">The opaque subject the certificate is for.</param>
/// <param name="Instance">The kernel instance the certificate is for.</param>
/// <param name="Identity">The identity in the certificate, <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;id&gt;/kernel/&lt;instance&gt;</c>.</param>
/// <param name="NotBefore">When the certificate becomes valid.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Reason">Why it was issued, <c language="text">enrollment</c> or <c language="text">renewal</c>.</param>
/// <param name="ActedBy">The identity that caused the issuance: the enrollment for a redemption, the presented identity for a renewal.</param>
[EventType]
public record FederationCertificateIssued(
    string EnrollmentId,
    string Subject,
    string Instance,
    string Identity,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Reason,
    string ActedBy);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The folded state of a certificate the federation authority issued. This is the registry of issued certificates.
/// </summary>
/// <param name="Serial">The serial number, as uppercase hex.</param>
/// <param name="EnrollmentId">The enrollment the certificate descends from.</param>
/// <param name="Subject">The opaque subject the certificate is for.</param>
/// <param name="Instance">The kernel instance the certificate is for.</param>
/// <param name="Identity">The identity in the certificate.</param>
/// <param name="NotBefore">When the certificate becomes valid.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Reason">Why it was issued, enrollment or renewal.</param>
/// <param name="Revoked">Whether the certificate has been revoked.</param>
public record FederationIssuedCertificateState(
    string Serial,
    string EnrollmentId,
    string Subject,
    string Instance,
    string Identity,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Reason,
    bool Revoked);

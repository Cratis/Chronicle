// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A certificate the authority kernel issued.
/// </summary>
/// <param name="Serial">The serial number, as uppercase hex.</param>
/// <param name="EnrollmentId">The enrollment the certificate descends from.</param>
/// <param name="Subject">The opaque subject the certificate is for.</param>
/// <param name="Instance">The kernel instance the certificate is for.</param>
/// <param name="Identity">The identity in the certificate.</param>
/// <param name="NotBefore">When the certificate becomes valid.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Reason">Why it was issued, <c language="text">enrollment</c> or <c language="text">renewal</c>.</param>
/// <param name="Status">The status: <c language="text">active</c>, <c language="text">expired</c> or <c language="text">revoked</c>.</param>
public record FederationCertificateInformation(
    string Serial,
    string EnrollmentId,
    string Subject,
    string Instance,
    string Identity,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Reason,
    string Status);

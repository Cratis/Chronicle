// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A certificate the federation authority issued. The registry of these is what a presented certificate is checked against.
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
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationIssuedCertificate(
    string Serial,
    string EnrollmentId,
    string Subject,
    string Instance,
    string Identity,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Reason,
    string Status)
{
    /// <summary>
    /// Gets every certificate the authority issued.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the issued certificates.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to decide expiry with.</param>
    /// <returns>The issued certificates.</returns>
    public static async Task<IEnumerable<FederationIssuedCertificate>> AllIssuedCertificates(IFederationCertificateRegistry registry, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var certificates = await registry.GetIssuedCertificates();
        return certificates.Select(_ => new FederationIssuedCertificate(
            _.Serial,
            _.EnrollmentId,
            _.Subject,
            _.Instance,
            _.Identity,
            _.NotBefore,
            _.NotAfter,
            _.Reason,
            StatusOf(_, now)));
    }

    static string StatusOf(FederationIssuedCertificateState certificate, DateTimeOffset now)
    {
        if (certificate.Revoked)
        {
            return "revoked";
        }

        return now >= certificate.NotAfter ? "expired" : "active";
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="IFederationEnrollmentTransport"/> that talks to an authority in the same process, and counts its calls.
/// </summary>
/// <param name="issuingAuthority">The authority to talk to.</param>
/// <param name="authorityCertificate">The authority certificate it serves.</param>
public class InProcessEnrollmentTransport(IFederationAuthority issuingAuthority, X509Certificate2 authorityCertificate) : IFederationEnrollmentTransport
{
    /// <summary>
    /// Gets or sets whether the authority presents a certificate that does not match the fingerprint in the code.
    /// </summary>
    public bool PresentsAnotherAuthority { get; set; }

    /// <summary>
    /// Gets the number of times an authority certificate was fetched.
    /// </summary>
    public int Fetches { get; private set; }

    /// <summary>
    /// Gets the number of times an enrollment was redeemed.
    /// </summary>
    public int Redemptions { get; private set; }

    /// <summary>
    /// Gets the number of times a certificate was renewed.
    /// </summary>
    public int Renewals { get; private set; }

    /// <inheritdoc/>
    public Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken)
    {
        Fetches++;
        return Task.FromResult<X509Certificate2?>(PresentsAnotherAuthority ? null : X509CertificateLoader.LoadCertificate(authorityCertificate.RawData));
    }

    /// <inheritdoc/>
    public Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken)
    {
        Redemptions++;
        return issuingAuthority.Redeem(request.EnrollmentId, request.Secret, Convert.FromBase64String(request.Csr));
    }

    /// <inheritdoc/>
    public Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken)
    {
        Renewals++;
        return issuingAuthority.Renew(clientCertificate.RawData, Convert.FromBase64String(request.Csr));
    }
}

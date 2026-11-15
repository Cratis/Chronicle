// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines how a kernel reaches the enrollment routes of a hosted federation authority.
/// </summary>
public interface IFederationEnrollmentTransport
{
    /// <summary>
    /// Fetch the authority certificate and check it against the fingerprint from an enrollment code. The response is public and
    /// is trusted only because it matches the fingerprint, so this is the one request that does not validate the server.
    /// </summary>
    /// <param name="endpoint">The public endpoint from the enrollment code.</param>
    /// <param name="authorityFingerprint">The fingerprint of the authority root from the enrollment code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authority certificate, or null when it could not be fetched or does not match the fingerprint.</returns>
    Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken);

    /// <summary>
    /// Redeem an enrollment. No client certificate is presented, and the server is trusted only when it chains to the authority root.
    /// </summary>
    /// <param name="endpoint">The public endpoint from the enrollment code.</param>
    /// <param name="authority">The authority root, as returned by <see cref="GetAuthority"/>.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The issuance, with an outcome that is not <see cref="FederationIssuanceOutcome.Issued"/> when the request was refused.</returns>
    Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Renew a certificate over mutual TLS.
    /// </summary>
    /// <param name="endpoint">The public endpoint of the authority.</param>
    /// <param name="authority">The authority root the server must chain to.</param>
    /// <param name="clientCertificate">The certificate to renew, with its private key.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The issuance, with an outcome that is not <see cref="FederationIssuanceOutcome.Issued"/> when the request was refused.</returns>
    Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken);
}

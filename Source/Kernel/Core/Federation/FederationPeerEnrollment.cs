// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationPeerEnrollment"/>.
/// </summary>
/// <remarks>
/// The key pair is generated here and the private key never leaves this kernel. The identity of the issued certificate is
/// decided by the authority from the enrollment, so the certificate request only proves possession of the key.
/// </remarks>
/// <param name="identities">The <see cref="IFederationEnrolledIdentities"/> holding this kernel's identity.</param>
/// <param name="transport">The <see cref="IFederationEnrollmentTransport"/> reaching the authority.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public sealed class FederationPeerEnrollment(
    IFederationEnrolledIdentities identities,
    IFederationEnrollmentTransport transport,
    TimeProvider timeProvider,
    ILogger<FederationPeerEnrollment> logger) : IFederationPeerEnrollment, IDisposable
{
    readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc/>
    public void Dispose() => _lock.Dispose();

    /// <inheritdoc/>
    public async Task<FederationRedemption> Redeem(string code, CancellationToken cancellationToken = default)
    {
        if (!FederationEnrollmentCode.TryParse(code, out var parsed))
        {
            throw new InvalidFederationEnrollment("the enrollment code is not valid");
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var current = await identities.Get();
            if (current is not null && current.AuthorityFingerprint == parsed.AuthorityFingerprint && current.NotAfter > now)
            {
                return new(current.Identity, current.Serial, current.NotAfter, current.Endpoint, true);
            }

            var endpoint = new Uri(parsed.Endpoint);
            using var authority = await transport.GetAuthority(endpoint, parsed.AuthorityFingerprint, cancellationToken)
                ?? throw new InvalidFederationEnrollment("the authority could not be reached, or it does not present the certificate the enrollment code names");

            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var csr = new CertificateRequest("CN=chronicle-federation-peer", key, HashAlgorithmName.SHA256).CreateSigningRequest();

            var issuance = await transport.Redeem(
                endpoint,
                authority,
                new(parsed.EnrollmentId, parsed.Secret, Convert.ToBase64String(csr)),
                cancellationToken);

            if (issuance.Outcome != FederationIssuanceOutcome.Issued)
            {
                logger.EnrollmentWasRefused(parsed.EnrollmentId);
                throw new InvalidFederationEnrollment("the authority did not accept the enrollment code. It may be unknown, expired or already used");
            }

            var installed = await Install(key, issuance, parsed.AuthorityFingerprint, parsed.Endpoint, FederationIssuanceReasons.Enrollment, now, null);
            logger.Enrolled(installed.Identity, installed.Serial, installed.NotAfter);
            return new(installed.Identity, installed.Serial, installed.NotAfter, installed.Endpoint, false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RenewIfDue(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var current = await identities.Get();
            var now = timeProvider.GetUtcNow();
            if (current is null || now < current.NotBefore + ((current.NotAfter - current.NotBefore) / 2))
            {
                return false;
            }

            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var csr = new CertificateRequest("CN=chronicle-federation-peer", key, HashAlgorithmName.SHA256).CreateSigningRequest();

            var issuance = await transport.Renew(
                new Uri(current.Endpoint),
                current.Authority,
                current.Certificate,
                new(Convert.ToBase64String(csr)),
                cancellationToken);

            if (issuance.Outcome != FederationIssuanceOutcome.Issued)
            {
                logger.RenewalWasRefused(current.Identity);
                return false;
            }

            var endpoint = Uri.TryCreate(issuance.Endpoint, UriKind.Absolute, out var moved) && moved.Scheme == Uri.UriSchemeHttps
                ? moved.ToString()
                : current.Endpoint;
            var installed = await Install(key, issuance, current.AuthorityFingerprint, endpoint, FederationIssuanceReasons.Renewal, now, current.Identity);

            logger.Renewed(installed.Identity, installed.Serial, installed.NotAfter);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task<FederationEnrolledIdentity> Install(ECDsa key, FederationIssuance issuance, string authorityFingerprint, string endpoint, string reason, DateTimeOffset now, string? expectedIdentity)
    {
        using var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(issuance.Certificate));
        using var authority = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(issuance.Authority));

        if (FederationCertificateAuthority.Fingerprint(authority) != authorityFingerprint)
        {
            throw new InvalidFederationEnrollment("the authority certificate does not match the fingerprint the enrollment code carries");
        }

        if (!FederationCertificateAuthority.ChainsTo(certificate, authority, now, FederationCertificateAuthority.ClientAuthentication))
        {
            throw new InvalidFederationEnrollment("the issued certificate does not chain to the authority");
        }

        using var certificateKey = certificate.GetECDsaPublicKey();
        if (certificateKey?.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(key.ExportSubjectPublicKeyInfo()) != true)
        {
            throw new InvalidFederationEnrollment("the issued certificate is not for the key this kernel generated");
        }

        var identity = FederationCertificateAuthority.GetIdentity(certificate);
        if (identity is null || identity != issuance.Identity)
        {
            throw new InvalidFederationEnrollment("the issued certificate does not carry the identity the authority reported");
        }

        if (expectedIdentity is not null && identity != expectedIdentity)
        {
            throw new InvalidFederationEnrollment("the renewed certificate carries a different identity");
        }

        await identities.Install(key, certificate, authority, endpoint, reason);
        return (await identities.Get())!;
    }
}

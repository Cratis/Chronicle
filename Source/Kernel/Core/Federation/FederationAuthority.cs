// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationAuthority"/>.
/// </summary>
/// <remarks>
/// The grain is a single activation, so requests are decided one at a time cluster-wide. Every issuance and revocation is
/// appended to the system event log before the answer leaves the grain, so there is no certificate in the world that the
/// registry does not know about. The token secret and every private key stay out of events and logs.
/// </remarks>
/// <param name="registry">The <see cref="IFederationCertificateRegistry"/>.</param>
/// <param name="keys">The <see cref="IFederationAuthorityKeys"/>.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
public class FederationAuthority(
    IFederationCertificateRegistry registry,
    IFederationAuthorityKeys keys,
    IOptions<ChronicleOptions> options,
    TimeProvider timeProvider,
    ILogger<FederationAuthority> logger) : Grain, IFederationAuthority
{
    static readonly TimeSpan _clockSkew = TimeSpan.FromMinutes(5);
    static readonly byte[] _unknownTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes("no such enrollment"));

    FederationHostedOptions Hosted => options.Value.Federation.Hosted;

    /// <inheritdoc/>
    public async Task<string> EnsureAuthority()
    {
        EnsureEnabled();
        return FederationCertificateAuthority.Fingerprint(await GetAuthority());
    }

    /// <inheritdoc/>
    public async Task<FederationEnrollmentCreatedResult> CreateEnrollment(string subject, string instance)
    {
        EnsureEnabled();
        if (!Uri.TryCreate(Hosted.PublicEndpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidFederationEnrollment("Federation:Hosted:PublicEndpoint must be an absolute https URI. It has no default.");
        }

        instance = string.IsNullOrWhiteSpace(instance) ? Guid.NewGuid().ToString("N")[..12] : instance;

        // Validates the segments before anything is created.
        _ = FederationIdentities.Create(Hosted.TrustDomain, subject, instance);

        var authority = await GetAuthority();
        var now = timeProvider.GetUtcNow();
        var id = Guid.NewGuid().ToString("N");
        var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
        var expiresAt = now.Add(Hosted.EnrollmentLifetime);
        var createdBy = ActingIdentity();

        await registry.Append(id, new FederationEnrollmentCreated(subject, instance, HashSecret(secret), now, expiresAt, createdBy));
        logger.EnrollmentCreated(id, subject, instance, expiresAt);

        var code = new FederationEnrollmentCode(endpoint.ToString(), FederationCertificateAuthority.Fingerprint(authority), id, secret);
        return new(id, code.Format(), expiresAt);
    }

    /// <inheritdoc/>
    public async Task RevokeEnrollment(string enrollmentId)
    {
        EnsureEnabled();
        var enrollment = await registry.FindEnrollment(enrollmentId)
            ?? throw new InvalidFederationEnrollment("the enrollment is not known");

        var revokedBy = ActingIdentity();
        if (!enrollment.Revoked)
        {
            await registry.Append(enrollmentId, new FederationEnrollmentRevoked(revokedBy));
        }

        foreach (var certificate in (await registry.GetIssuedCertificates()).Where(_ => _.EnrollmentId == enrollmentId && !_.Revoked))
        {
            await registry.Append(certificate.Serial, new FederationCertificateRevoked(certificate.Identity, revokedBy));
            logger.CertificateRevoked(certificate.Serial, certificate.Identity);
        }

        logger.EnrollmentRevoked(enrollmentId);
    }

    /// <inheritdoc/>
    public async Task<FederationIssuance> Redeem(string enrollmentId, string secret, byte[] certificateRequest)
    {
        if (!Hosted.Enabled)
        {
            return FederationIssuance.Rejected;
        }

        var now = timeProvider.GetUtcNow();
        var enrollment = await registry.FindEnrollment(enrollmentId ?? string.Empty);

        // The comparison always runs, against a fixed hash when the enrollment is unknown, so the time taken does not tell whether it exists.
        var provided = SHA256.HashData(Encoding.UTF8.GetBytes(secret ?? string.Empty));
        var expected = enrollment is null ? _unknownTokenHash : Convert.FromBase64String(enrollment.TokenHash);
        var secretMatches = CryptographicOperations.FixedTimeEquals(provided, expected);

        if (enrollment is null || !secretMatches || enrollment.Redeemed || enrollment.Revoked || now >= enrollment.ExpiresAt)
        {
            logger.EnrollmentRefused();
            return FederationIssuance.Rejected;
        }

        var identity = FederationIdentities.Create(Hosted.TrustDomain, enrollment.Subject, enrollment.Instance);
        return await Issue(enrollment.Id, enrollment.Subject, enrollment.Instance, identity, certificateRequest, FederationIssuanceReasons.Enrollment, $"enrollment:{enrollment.Id}");
    }

    /// <inheritdoc/>
    public async Task<FederationIssuance> Renew(byte[] presentedCertificate, byte[] certificateRequest)
    {
        if (!Hosted.Enabled)
        {
            return FederationIssuance.Rejected;
        }

        X509Certificate2 presented;
        try
        {
            presented = X509CertificateLoader.LoadCertificate(presentedCertificate);
        }
        catch (CryptographicException)
        {
            return FederationIssuance.Rejected;
        }

        using (presented)
        {
            var authority = await GetAuthority();
            var now = timeProvider.GetUtcNow();
            if (!FederationCertificateAuthority.ChainsTo(presented, authority, now, FederationCertificateAuthority.ClientAuthentication))
            {
                logger.RenewalRefused();
                return FederationIssuance.Rejected;
            }

            var issued = await registry.FindIssuedCertificate(presented.SerialNumber);
            if (issued?.Revoked != false || FederationCertificateAuthority.GetIdentity(presented) != issued.Identity)
            {
                logger.RenewalRefused();
                return FederationIssuance.Rejected;
            }

            var renewed = await Issue(issued.EnrollmentId, issued.Subject, issued.Instance, issued.Identity, certificateRequest, FederationIssuanceReasons.Renewal, issued.Identity);
            if (renewed.Outcome == FederationIssuanceOutcome.Issued)
            {
                // One live certificate per instance: the one that was renewed is superseded, so a leaked older copy can neither renew again nor connect.
                await registry.Append(issued.Serial, new FederationCertificateRevoked(issued.Identity, "renewal"));
                logger.CertificateRevoked(issued.Serial, issued.Identity);
            }

            return renewed;
        }
    }

    static string HashSecret(string secret) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    static string ActingIdentity() => RequestContext.Get(WellKnownKeys.UserIdentity) is string user && !string.IsNullOrWhiteSpace(user) ? user : "system";

    void EnsureEnabled()
    {
        if (!Hosted.Enabled)
        {
            throw new InvalidFederationEnrollment("hosted federation is not enabled. Set Federation:Hosted:Enabled.");
        }
    }

    async Task<X509Certificate2> GetAuthority()
    {
        var (certificate, created) = await keys.GetOrCreate();
        if (created || await registry.GetAuthority() is null)
        {
            await registry.Append("authority", new FederationAuthorityCreated(
                FederationCertificateAuthority.Fingerprint(certificate),
                Hosted.TrustDomain,
                new DateTimeOffset(certificate.NotBefore),
                new DateTimeOffset(certificate.NotAfter)));
            logger.AuthorityCreated(FederationCertificateAuthority.Fingerprint(certificate));
        }

        return certificate;
    }

    async Task<FederationIssuance> Issue(string enrollmentId, string subject, string instance, string identity, byte[] certificateRequest, string reason, string actedBy)
    {
        var authority = await GetAuthority();
        var now = timeProvider.GetUtcNow();
        var authorityExpires = new DateTimeOffset(authority.NotAfter);
        if (now >= authorityExpires)
        {
            // An expired authority cannot be chained to, so nothing it signs is of any use. The operator has to replace the authority; see the federation documentation.
            logger.AuthorityExpired(authorityExpires);
            return FederationIssuance.Rejected;
        }

        // A certificate never outlives the authority that signed it.
        var notAfter = now.Add(Hosted.CertificateLifetime);
        if (notAfter > authorityExpires)
        {
            notAfter = authorityExpires;
        }

        X509Certificate2 certificate;
        try
        {
            certificate = FederationCertificateAuthority.IssueClient(authority, certificateRequest, identity, now.Subtract(_clockSkew), notAfter);
        }
        catch (InvalidFederationCertificateRequest)
        {
            logger.CertificateRequestInvalid();
            return FederationIssuance.InvalidRequest;
        }

        using (certificate)
        {
            // A code is single use. The claim is made after the request was found to be valid, so a bad request does not burn the code, and
            // it is the event log that decides between concurrent redemptions, not the order this grain happens to serve them in.
            if (reason == FederationIssuanceReasons.Enrollment && !await registry.ClaimRedemption(enrollmentId, certificate.SerialNumber))
            {
                logger.EnrollmentRefused();
                return FederationIssuance.Rejected;
            }

            // Recorded before it is handed out: a certificate that is not in the registry is never accepted.
            await registry.Append(certificate.SerialNumber, new FederationCertificateIssued(enrollmentId, subject, instance, identity, new DateTimeOffset(certificate.NotBefore), new DateTimeOffset(certificate.NotAfter), reason, actedBy));
            logger.CertificateIssued(certificate.SerialNumber, identity, new DateTimeOffset(certificate.NotAfter), reason);

            return new(
                FederationIssuanceOutcome.Issued,
                Convert.ToBase64String(certificate.RawData),
                Convert.ToBase64String(authority.RawData),
                identity,
                certificate.SerialNumber,
                new DateTimeOffset(certificate.NotAfter),
                Hosted.PublicEndpoint ?? string.Empty);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationPeerAdmission"/>.
/// </summary>
/// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the issued certificates and the policies.</param>
/// <param name="keys">The <see cref="IFederationAuthorityKeys"/> holding the authority certificate.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
[Singleton]
public class FederationPeerAdmission(
    IFederationCertificateRegistry registry,
    IFederationAuthorityKeys keys,
    IOptions<ChronicleOptions> options,
    TimeProvider timeProvider) : IFederationPeerAdmission
{
    /// <inheritdoc/>
    public async Task<FederationAdmission> Admit(X509Certificate2? certificate)
    {
        var federation = options.Value.Federation;
        if (!federation.Enabled || !federation.Hosted.Enabled)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.NotEnabled);
        }

        if (certificate is null)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.NoCertificate);
        }

        if (await registry.GetAuthority() is null)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.NoAuthority);
        }

        var (authority, _) = await keys.GetOrCreate();
        if (!FederationCertificateAuthority.ChainsTo(certificate, authority, timeProvider.GetUtcNow(), FederationCertificateAuthority.ClientAuthentication))
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.NotChained);
        }

        var identity = FederationCertificateAuthority.GetIdentity(certificate);
        if (!FederationIdentities.TryParse(identity, out var trustDomain, out var subject, out var instance))
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.WrongIdentity);
        }

        if (!string.Equals(trustDomain, federation.Hosted.TrustDomain, StringComparison.Ordinal))
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.WrongTrustDomain);
        }

        var issued = await registry.FindIssuedCertificate(certificate.SerialNumber);
        if (issued is null)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.NotIssued);
        }

        if (issued.Revoked)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.CertificateRevoked);
        }

        if (issued.Identity != identity || issued.Subject != subject || issued.Instance != instance)
        {
            return FederationAdmission.Refused(FederationAdmissionRefusal.SubjectMismatch);
        }

        var policy = await registry.FindPolicy(subject);
        return policy?.Status switch
        {
            null => FederationAdmission.Refused(FederationAdmissionRefusal.NoPolicy),
            FederationPolicyStatus.Suspended => FederationAdmission.Refused(FederationAdmissionRefusal.Suspended),
            FederationPolicyStatus.Revoked => FederationAdmission.Refused(FederationAdmissionRefusal.Revoked),
            _ => new FederationAdmission(FederationAdmissionRefusal.None, subject, instance, policy)
        };
    }
}

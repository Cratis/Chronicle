// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A hosted federation authority, the policies of its subjects and the admission of peers by certificate.
/// </summary>
public class a_peer_admission : a_federation_authority
{
    protected const string TargetStore = "customers";
    protected const string TargetNamespace = "acme-prod";

    protected FederationPeerAdmission _admission;
    protected FederationPolicies _policies;
    protected FederationAdmission _admitted;
    protected List<X509Certificate2> _certificates;

    void Establish()
    {
        _certificates = [];
        _policies = new(_registry, NullLogger<FederationPolicies>.Instance);
        _admission = new(_registry, _keys, Options.Create(_options), _time);
    }

    void Destroy()
    {
        foreach (var certificate in _certificates)
        {
            certificate.Dispose();
        }
    }

    protected async Task<X509Certificate2> Issued(string subject = "acme", string instance = "prod")
    {
        var (issuance, key) = await Redeem(await Enroll(subject, instance));
        key.Dispose();
        return Track(CertificateOf(issuance));
    }

    protected Task SetPolicy(string subject = "acme", FederationDirection direction = FederationDirection.AcceptorOutbound, bool allPublic = true, params string[] eventTypes) =>
        _policies.Set(subject, direction, TargetStore, TargetNamespace, allPublic, eventTypes, "operator");

    protected X509Certificate2 SignedBy(X509Certificate2 authority, string identity)
    {
        using var key = NewKey();
        var now = _time.GetUtcNow();
        return Track(FederationCertificateAuthority.IssueClient(authority, Csr(key), identity, now.AddMinutes(-1), now.AddDays(14)));
    }

    protected X509Certificate2 AnotherAuthority() =>
        Track(FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(20)));

    protected Task<FederationAdmission> Admit(X509Certificate2? certificate) => _admission.Admit(certificate);

    X509Certificate2 Track(X509Certificate2 certificate)
    {
        _certificates.Add(certificate);
        return certificate;
    }
}

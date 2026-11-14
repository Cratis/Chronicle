// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_renewing_a_certificate : given.a_federation_authority
{
    FederationIssuance _original;
    FederationIssuance _renewal;
    X509Certificate2 _originalCertificate;
    X509Certificate2 _renewedCertificate;
    ECDsa _originalKey;
    ECDsa _newKey;
    FederationIssuedCertificateState _registered;

    async Task Because()
    {
        var code = await Enroll("acme", "prod");
        (_original, _originalKey) = await Redeem(code);
        _originalCertificate = CertificateOf(_original);

        _time.Advance(TimeSpan.FromDays(8));
        _newKey = NewKey();
        _renewal = await _authority.Renew(_originalCertificate.RawData, Csr(_newKey));
        _renewedCertificate = CertificateOf(_renewal);
        _registered = await _registry.FindIssuedCertificate(_renewedCertificate.SerialNumber);
    }

    void Destroy()
    {
        _originalCertificate.Dispose();
        _renewedCertificate.Dispose();
        _originalKey.Dispose();
        _newKey.Dispose();
    }

    [Fact] void should_issue_a_certificate() => _renewal.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_keep_the_identity() => FederationCertificateAuthority.GetIdentity(_renewedCertificate).ShouldEqual(FederationCertificateAuthority.GetIdentity(_originalCertificate));
    [Fact] void should_use_the_new_key() => _renewedCertificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo().ShouldEqual(_newKey.ExportSubjectPublicKeyInfo());
    [Fact] void should_not_reuse_the_old_key() => _renewedCertificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo().ShouldNotEqual(_originalKey.ExportSubjectPublicKeyInfo());
    [Fact] void should_have_a_new_serial_number() => _renewedCertificate.SerialNumber.ShouldNotEqual(_originalCertificate.SerialNumber);
    [Fact] void should_last_a_full_lifetime_from_now() => (new DateTimeOffset(_renewedCertificate.NotAfter) - _time.GetUtcNow()).ShouldEqual(TimeSpan.FromDays(14));
    [Fact] void should_record_the_renewal() => _registry.Appended.Select(_ => _.Event).OfType<FederationCertificateIssued>().Last().Reason.ShouldEqual(FederationIssuanceReasons.Renewal);
    [Fact] void should_record_the_presented_identity_as_the_one_that_acted() => _registry.Appended.Select(_ => _.Event).OfType<FederationCertificateIssued>().Last().ActedBy.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_register_the_new_serial_number() => _registered.ShouldNotBeNull();
    [Fact] void should_report_the_current_endpoint() => _renewal.Endpoint.ShouldEqual("https://federation.example.com:35100");
}

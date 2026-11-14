// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_redeeming_an_enrollment : given.a_federation_authority
{
    FederationIssuance _issuance;
    X509Certificate2 _certificate;
    FederationCertificateIssued _event;
    string _eventSourceId;
    System.Security.Cryptography.ECDsa _key;
    FederationIssuedCertificateState _registered;

    async Task Because()
    {
        var code = await Enroll("acme", "prod");
        (_issuance, _key) = await Redeem(code);
        _certificate = CertificateOf(_issuance);
        (_eventSourceId, var @event) = _registry.Appended.Single(_ => _.Event is FederationCertificateIssued);
        _event = (FederationCertificateIssued)@event;
        _registered = await _registry.FindIssuedCertificate(_certificate.SerialNumber);
    }

    void Destroy()
    {
        _certificate.Dispose();
        _key.Dispose();
    }

    [Fact] void should_issue_a_certificate() => _issuance.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_put_the_identity_from_the_enrollment_in_the_certificate() => FederationCertificateAuthority.GetIdentity(_certificate).ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_report_the_identity() => _issuance.Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_chain_to_the_authority() => FederationCertificateAuthority.ChainsTo(_certificate, _keys.Authority, _time.GetUtcNow(), FederationCertificateAuthority.ClientAuthentication).ShouldBeTrue();
    [Fact] void should_be_for_the_key_that_made_the_request() => _certificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo().ShouldEqual(_key.ExportSubjectPublicKeyInfo());
    [Fact] void should_last_fourteen_days_by_default() => (new DateTimeOffset(_certificate.NotAfter) - _time.GetUtcNow()).ShouldEqual(TimeSpan.FromDays(14));
    [Fact] void should_not_be_a_certificate_authority() => _certificate.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority.ShouldBeFalse();
    [Fact] void should_only_be_usable_for_client_authentication() => _certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single().EnhancedKeyUsages.OfType<System.Security.Cryptography.Oid>().Select(_ => _.Value).Single().ShouldEqual("1.3.6.1.5.5.7.3.2");
    [Fact] void should_register_the_serial_number_under_the_event_source_id() => _eventSourceId.ShouldEqual(_certificate.SerialNumber);
    [Fact] void should_record_the_subject_and_expiry_in_the_event() => (_event.Subject, _event.Instance, _event.NotAfter).ShouldEqual(("acme", "prod", new DateTimeOffset(_certificate.NotAfter)));
    [Fact] void should_record_that_it_came_from_an_enrollment() => _event.Reason.ShouldEqual(FederationIssuanceReasons.Enrollment);
    [Fact] void should_record_who_acted() => _event.ActedBy.StartsWith("enrollment:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_know_the_certificate_in_the_registry() => _registered.ShouldNotBeNull();
    [Fact] void should_report_the_endpoint() => _issuance.Endpoint.ShouldEqual("https://federation.example.com:35100");
}

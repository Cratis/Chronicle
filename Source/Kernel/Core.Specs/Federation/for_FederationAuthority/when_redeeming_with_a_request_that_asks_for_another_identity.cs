// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_redeeming_with_a_request_that_asks_for_another_identity : given.a_federation_authority
{
    FederationIssuance _issuance;
    X509Certificate2 _certificate;

    async Task Because()
    {
        var code = await Enroll("acme", "prod");
        using var key = NewKey();

        // The request names another customer, in its subject and in its subject alternative name, and asks to be a certificate authority.
        var request = new CertificateRequest("CN=spiffe://example/customer/victim/kernel/prod, O=victim", key, HashAlgorithmName.SHA256);
        var names = new SubjectAlternativeNameBuilder();
        names.AddUri(new Uri("spiffe://example/customer/victim/kernel/prod"));
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        _issuance = await _authority.Redeem(code.EnrollmentId, code.Secret, request.CreateSigningRequest());
        _certificate = CertificateOf(_issuance);
    }

    void Destroy() => _certificate.Dispose();

    [Fact] void should_issue_the_certificate() => _issuance.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_take_the_identity_from_the_enrollment() => FederationCertificateAuthority.GetIdentity(_certificate).ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_not_take_the_subject_from_the_request() => _certificate.Subject.ShouldNotContain("victim");
    [Fact] void should_not_let_the_request_make_it_a_certificate_authority() => _certificate.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority.ShouldBeFalse();
    [Fact] void should_register_only_the_identity_of_the_enrollment() => _registry.Appended.Select(_ => _.Event).OfType<FederationCertificateIssued>().Single().Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
}

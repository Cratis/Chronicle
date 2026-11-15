// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_revoking_an_enrollment : given.a_federation_authority
{
    X509Certificate2 _certificate;
    FederationEnrollmentCode _code;
    Exception _unknown;
    FederationIssuedCertificateState _registered;

    async Task Because()
    {
        _code = await Enroll("acme", "prod");
        var (issuance, key) = await Redeem(_code);
        key.Dispose();
        _certificate = CertificateOf(issuance);

        await _authority.RevokeEnrollment(_code.EnrollmentId);
        await _authority.RevokeEnrollment(_code.EnrollmentId);
        _unknown = await Catch.Exception(() => _authority.RevokeEnrollment("nothing"));
        _registered = await _registry.FindIssuedCertificate(_certificate.SerialNumber);
    }

    void Destroy() => _certificate.Dispose();

    [Fact] void should_record_the_revocation_of_the_enrollment_once() => _registry.Appended.Count(_ => _.Event is FederationEnrollmentRevoked).ShouldEqual(1);
    [Fact] void should_record_the_revocation_of_the_certificate_once_under_its_serial_number() => _registry.Appended.Single(_ => _.Event is FederationCertificateRevoked).EventSourceId.ShouldEqual(_certificate.SerialNumber);
    [Fact] void should_record_the_identity_that_was_revoked() => ((FederationCertificateRevoked)_registry.Appended.Single(_ => _.Event is FederationCertificateRevoked).Event).Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_mark_the_certificate_as_revoked_in_the_registry() => _registered.Revoked.ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_enrollment() => _unknown.ShouldBeOfExactType<InvalidFederationEnrollment>();
}

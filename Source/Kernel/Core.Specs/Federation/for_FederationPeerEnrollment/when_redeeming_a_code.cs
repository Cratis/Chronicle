// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_redeeming_a_code : given.a_peer_enrollment
{
    string _code;
    FederationRedemption _redemption;
    FederationEnrolledIdentity _identity;

    async Task Because()
    {
        _code = (await _authority.CreateEnrollment("acme", "prod")).Code;
        _redemption = await _peer.Redeem(_code);
        _identity = await _identities.Get();
    }

    [Fact] void should_report_the_identity_from_the_enrollment() => _redemption.Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_not_report_that_it_was_already_enrolled() => _redemption.AlreadyEnrolled.ShouldBeFalse();
    [Fact] void should_hold_the_certificate_with_a_private_key() => _identity.Certificate.HasPrivateKey.ShouldBeTrue();
    [Fact] void should_hold_a_certificate_that_chains_to_the_authority() => FederationCertificateAuthority.ChainsTo(_identity.Certificate, _identity.Authority, _time.GetUtcNow()).ShouldBeTrue();
    [Fact] void should_remember_the_fingerprint_of_the_authority() => _identity.AuthorityFingerprint.ShouldEqual(FederationCertificateAuthority.Fingerprint(_keys.Authority));
    [Fact] void should_remember_the_endpoint() => _identity.Endpoint.ShouldEqual("https://federation.example.com:35100/");
    [Fact] void should_have_installed_it_as_an_enrollment() => _identities.Reasons.Single().ShouldEqual(FederationIssuanceReasons.Enrollment);
    [Fact] void should_not_log_the_code() => _logger.Contains(_code).ShouldBeFalse();
    [Fact] void should_not_log_the_secret() => _logger.Contains(FederationEnrollmentCode.TryParse(_code, out var parsed) ? parsed.Secret : string.Empty).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_the_code_cannot_be_trusted : given.a_peer_enrollment
{
    Exception _notACode;
    Exception _wrongAuthority;
    Exception _refused;
    int _redemptionsWhenTheAuthorityWasWrong;
    string _usedSecret;
    FederationEnrolledIdentity _enrolled;

    async Task Because()
    {
        _notACode = await Catch.Exception(() => _peer.Redeem("not a code"));

        var code = (await _authority.CreateEnrollment("acme", "prod")).Code;
        _transport.PresentsAnotherAuthority = true;
        _wrongAuthority = await Catch.Exception(() => _peer.Redeem(code));
        _redemptionsWhenTheAuthorityWasWrong = _transport.Redemptions;
        _transport.PresentsAnotherAuthority = false;

        var used = (await _authority.CreateEnrollment("acme", "used")).Code;
        FederationEnrollmentCode.TryParse(used, out var parsed).ShouldBeTrue();
        _usedSecret = parsed.Secret;
        using var key = NewKey();
        await _authority.Redeem(parsed.EnrollmentId, parsed.Secret, Csr(key));
        _refused = await Catch.Exception(() => _peer.Redeem(used));
        _enrolled = await _identities.Get();
    }

    [Fact] void should_refuse_something_that_is_not_a_code() => _notACode.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_an_authority_that_does_not_match_the_fingerprint_in_the_code() => _wrongAuthority.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_not_send_the_token_to_an_authority_it_cannot_trust() => _redemptionsWhenTheAuthorityWasWrong.ShouldEqual(0);
    [Fact] void should_refuse_a_code_the_authority_does_not_accept() => _refused.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_not_become_enrolled() => _enrolled.ShouldBeNull();
    [Fact] void should_not_log_the_secret_of_a_code_that_was_refused() => _logger.Contains(_usedSecret).ShouldBeFalse();
}

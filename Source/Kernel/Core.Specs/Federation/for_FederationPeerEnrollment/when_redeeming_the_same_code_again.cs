// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_redeeming_the_same_code_again : given.a_peer_enrollment
{
    FederationRedemption _first;
    FederationRedemption _second;
    int _redemptionsAfterTheFirst;

    async Task Because()
    {
        var code = (await _authority.CreateEnrollment("acme", "prod")).Code;
        _first = await _peer.Redeem(code);
        _redemptionsAfterTheFirst = _transport.Redemptions;
        _second = await _peer.Redeem(code);
    }

    [Fact] void should_redeem_the_first_time() => _first.AlreadyEnrolled.ShouldBeFalse();
    [Fact] void should_report_that_it_is_already_enrolled() => _second.AlreadyEnrolled.ShouldBeTrue();
    [Fact] void should_return_the_same_certificate() => _second.Serial.ShouldEqual(_first.Serial);
    [Fact] void should_not_contact_the_authority_again() => _transport.Redemptions.ShouldEqual(_redemptionsAfterTheFirst);
    [Fact] void should_install_the_identity_once() => _identities.Reasons.Count.ShouldEqual(1);
}

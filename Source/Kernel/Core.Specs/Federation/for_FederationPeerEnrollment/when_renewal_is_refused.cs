// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_renewal_is_refused : given.a_peer_enrollment
{
    bool _renewed;
    string _serialBefore;
    string _serialAfter;
    bool _notEnrolledRenewed;

    async Task Because()
    {
        _notEnrolledRenewed = await _peer.RenewIfDue();

        var created = await _authority.CreateEnrollment("acme", "prod");
        await _peer.Redeem(created.Code);
        _serialBefore = (await _identities.Get()).Serial;

        await _authority.RevokeEnrollment(created.Id);
        _time.Advance(TimeSpan.FromDays(8));
        _renewed = await _peer.RenewIfDue();
        _serialAfter = (await _identities.Get()).Serial;
    }

    [Fact] void should_have_nothing_to_renew_when_not_enrolled() => _notEnrolledRenewed.ShouldBeFalse();
    [Fact] void should_not_report_a_renewal() => _renewed.ShouldBeFalse();
    [Fact] void should_keep_the_certificate_it_has() => _serialAfter.ShouldEqual(_serialBefore);
}

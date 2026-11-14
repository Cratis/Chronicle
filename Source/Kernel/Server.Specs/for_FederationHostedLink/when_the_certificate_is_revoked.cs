// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

namespace Cratis.Chronicle.Server.for_FederationHostedLink;

public class when_the_certificate_is_revoked : given.a_hosted_federation_link
{
    bool _before;

    async Task Because()
    {
        if (Skip)
        {
            return;
        }

        Add(_operatorOutbox, 0);
        Add(_customerOutbox, 0);
        _before = await Drive(() => Count(_customerInbox) >= 1 && Count(_operatorInbox) >= 1);

        _certificateRevoked = true;
        Add(_operatorOutbox, 1, 2);
        Add(_customerOutbox, 1, 2);
        await Settle(TimeSpan.FromSeconds(1));
    }

    [Tls13Fact] void should_have_delivered_in_both_directions_before() => _before.ShouldBeTrue();
    [Tls13Fact] void should_stop_delivering_to_the_dialer() => _customerInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL]);
    [Tls13Fact] void should_stop_accepting_from_the_dialer() => _operatorInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Federation;
using Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

namespace Cratis.Chronicle.Server.for_FederationHostedLink;

public class when_one_direction_is_revoked : given.a_hosted_federation_link
{
    bool _before;
    bool _afterRevocation;

    async Task Because()
    {
        if (Skip)
        {
            return;
        }

        Add(_operatorOutbox, 0);
        Add(_customerOutbox, 0);
        _before = await Drive(() => Count(_customerInbox) >= 1 && Count(_operatorInbox) >= 1);

        // The operator revokes what the customer may produce; what the customer may consume stays.
        _policy = _policy with { Entries = [.. _policy.Entries.Where(_ => _.Direction == FederationDirection.AcceptorOutbound)] };
        Add(_customerOutbox, 1, 2);
        Add(_operatorOutbox, 1, 2);
        _afterRevocation = await Drive(() => Count(_customerInbox) >= 3);
        await Settle(TimeSpan.FromMilliseconds(500));
    }

    [Tls13Fact] void should_have_delivered_in_both_directions_before() => _before.ShouldBeTrue();
    [Tls13Fact] void should_keep_delivering_to_the_dialer() => _afterRevocation.ShouldBeTrue();
    [Tls13Fact] void should_receive_everything_the_acceptor_produced() => _customerInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL, 2UL]);
    [Tls13Fact] void should_stop_accepting_what_the_dialer_produces() => _operatorInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL]);
    [Tls13Fact] void should_not_need_a_new_connection_for_the_direction_that_stays() => Connections().ShouldEqual(1);
}

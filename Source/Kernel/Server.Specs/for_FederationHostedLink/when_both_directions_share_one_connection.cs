// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

namespace Cratis.Chronicle.Server.for_FederationHostedLink;

public class when_both_directions_share_one_connection : given.a_hosted_federation_link
{
    bool _delivered;

    async Task Because()
    {
        if (Skip)
        {
            return;
        }

        Add(_operatorOutbox, 0, 1, 2);
        Add(_customerOutbox, 0, 1);
        _delivered = await Drive(() => Count(_customerInbox) >= 3 && Count(_operatorInbox) >= 2);
        await Settle(TimeSpan.FromMilliseconds(300));
    }

    [Tls13Fact] void should_deliver_in_both_directions() => _delivered.ShouldBeTrue();
    [Tls13Fact] void should_deliver_the_events_of_the_acceptor_to_the_dialer_exactly_once_and_in_order() => _customerInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL, 2UL]);
    [Tls13Fact] void should_deliver_the_events_of_the_dialer_to_the_acceptor_exactly_once_and_in_order() => _operatorInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL]);
    [Tls13Fact] void should_name_the_dialer_as_the_origin_of_what_it_produced() => _operatorInbox.TrueForAll(_ => _.Origin == $"hosted-{Subject}").ShouldBeTrue();
    [Tls13Fact] void should_name_the_acceptor_as_the_origin_of_what_it_produced() => _customerInbox.TrueForAll(_ => _.Origin == OperatorStore).ShouldBeTrue();
    [Tls13Fact] void should_append_to_the_target_of_the_policy_and_nowhere_else() => _operatorInboxTargets.Distinct().ToArray().ShouldEqual([(OperatorStore, OperatorNamespace)]);
    [Tls13Fact] void should_have_used_one_connection_for_both() => Connections().ShouldEqual(1);
}

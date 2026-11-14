// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

namespace Cratis.Chronicle.Server.for_FederationHostedLink;

public class when_the_connection_is_lost : given.a_hosted_federation_link
{
    bool _before;
    bool _after;
    int _connectionsBefore;

    async Task Because()
    {
        if (Skip)
        {
            return;
        }

        Add(_operatorOutbox, 0, 1);
        Add(_customerOutbox, 0, 1);
        _before = await Drive(() => Count(_customerInbox) >= 2 && Count(_operatorInbox) >= 2);
        _connectionsBefore = Connections();

        AbortConnections();
        Add(_operatorOutbox, 2, 3);
        Add(_customerOutbox, 2, 3);
        _after = await Drive(() => Count(_customerInbox) >= 4 && Count(_operatorInbox) >= 4);
        await Quiesce();
    }

    [Tls13Fact] void should_have_delivered_in_both_directions_before_the_loss() => _before.ShouldBeTrue();
    [Tls13Fact] void should_have_used_one_connection_before_the_loss() => _connectionsBefore.ShouldEqual(1);
    [Tls13Fact] void should_resume_in_both_directions() => _after.ShouldBeTrue();
    [Tls13Fact] void should_reconnect_on_a_new_connection_for_both_directions() => Connections().ShouldEqual(2);
    [Tls13Fact] void should_lose_nothing_towards_the_dialer() => _customerInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL, 2UL, 3UL]);
    [Tls13Fact] void should_lose_nothing_towards_the_acceptor() => _operatorInbox.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL, 2UL, 3UL]);
}

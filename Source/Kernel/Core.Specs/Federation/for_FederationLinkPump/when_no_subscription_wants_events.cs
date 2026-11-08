// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_no_subscription_wants_events : given.a_link_pump
{
    TimeSpan? _delay;

    void Establish() => _subscriptions.Subscriptions.Clear();

    async Task Because() => _delay = await _pump.Step(default);

    [Fact] void should_pull_nothing() => _pull.Requests.ShouldBeEmpty();
    [Fact] void should_not_even_connect() => _clients.Created.ShouldEqual(0);
    [Fact] void should_be_idle() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Idle);
    [Fact] void should_check_again_after_the_poll_interval() => _delay.ShouldEqual(_timing.PollInterval);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_client_has_unsubscribed : given.a_subscribed_reactor_grain
{
    async Task Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        await _grain.Unsubscribe();
    }

    Task Because() => _silo.FireAllTimersAsync();

    [Fact] async Task should_not_check_with_the_observer() => await _observer.DidNotReceive().KnowsClient(_client.ConnectionId);
    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
    [Fact] void should_stop_keeping_the_subscription() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}

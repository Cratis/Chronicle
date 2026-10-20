// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_observer_lost_the_client : given.a_subscribed_reactor_grain
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(false);

    Task Because() => _silo.FireAllTimersAsync();

    [Fact] Task should_restore_the_subscription_of_the_client() => WasRestored(1);
    [Fact] void should_keep_checking() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}

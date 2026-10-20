// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_client_has_disconnected : given.a_subscribed_reactor_grain
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _connectedClients.IsConnected(_client.ConnectionId).Returns(false);
    }

    Task Because() => _silo.FireAllTimersAsync();

    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
    [Fact] void should_stop_keeping_the_subscription() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}

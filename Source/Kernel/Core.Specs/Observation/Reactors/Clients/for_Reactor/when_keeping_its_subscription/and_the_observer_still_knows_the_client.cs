// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_observer_still_knows_the_client : given.a_subscribed_reactor_grain
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    Task Because() => _silo.FireAllTimersAsync();

    [Fact] async Task should_check_with_the_observer() => await _observer.Received(1).KnowsClient(_client.ConnectionId);
    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
}

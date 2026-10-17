// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_active_client_is_gone_and_a_superseded_client_is_connected : for_Observer.given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;

    void Establish()
    {
        _connectedClients.IsConnected(_activeClient.ConnectionId).Returns(false);
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_use_the_event_types_of_the_superseded_client() => _subscription.EventTypes.ShouldContainOnly(superseded_event_type);
    [Fact] void should_target_the_superseded_client() => _subscription.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_supersededClient.ConnectionId);
}

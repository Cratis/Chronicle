// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_the_departure_is_notified_after_hand_over : given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;
    bool _subscribed;

    async Task Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        await _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);
    }

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);
        _subscription = await _observer.GetSubscription();
        _subscribed = await _observer.IsSubscribed();
    }

    [Fact] void should_still_be_subscribed() => _subscribed.ShouldBeTrue();
    [Fact] void should_keep_the_promoted_client_as_the_target() => _subscription.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_supersededClient.ConnectionId);
}

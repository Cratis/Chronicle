// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_the_superseded_client_disconnects_while_the_other_is_active : given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;
    bool _subscribedAfterActiveClientLeft;

    void Establish() => _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_supersededClient.ConnectionId);
        _subscription = await _observer.GetSubscription();

        await _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);
        _subscribedAfterActiveClientLeft = await _observer.IsSubscribed();
    }

    [Fact] void should_keep_the_active_subscription() => _subscription.EventTypes.ShouldContainOnly(active_event_type);
    [Fact] void should_keep_the_active_client_as_the_target() => _subscription.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_activeClient.ConnectionId);
    [Fact] void should_have_forgotten_the_superseded_registration() => _subscribedAfterActiveClientLeft.ShouldBeFalse();
}

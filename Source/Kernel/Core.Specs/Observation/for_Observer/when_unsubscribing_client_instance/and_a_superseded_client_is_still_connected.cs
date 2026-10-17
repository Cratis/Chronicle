// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_a_superseded_client_is_still_connected : given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;

    void Establish() => _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_use_the_event_types_of_the_superseded_client() => _subscription.EventTypes.ShouldContainOnly(superseded_event_type);
    [Fact] void should_have_a_single_target() => _subscription.Targets.Count.ShouldEqual(1);
    [Fact] void should_target_the_superseded_client() => _subscription.Targets[0].ConnectedClient!.ConnectionId.ShouldEqual(_supersededClient.ConnectionId);
    [Fact] void should_update_the_definition_to_the_event_types_of_the_superseded_client() => _definitionStorage.State.EventTypes.ShouldContainOnly(superseded_event_type);
}

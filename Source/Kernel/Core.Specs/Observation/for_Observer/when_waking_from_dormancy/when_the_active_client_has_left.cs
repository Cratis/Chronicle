// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// The client that held the subscription left while the observer was dormant: the standby the record names takes over, under
/// the definition it registered with, instead of the observer being disconnected.
/// </summary>
public class when_the_active_client_has_left : given.a_dormant_client_observer_with_a_standby
{
    ObserverSubscription _subscription;

    void Establish() => _connectedClients.IsConnected(_activeClient.ConnectionId).Returns(false);

    async Task Because()
    {
        await Wake();
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_have_a_single_target() => _subscription.Targets.Count.ShouldEqual(1);
    [Fact] void should_target_the_standby() => _subscription.Targets[0].ConnectedClient!.ConnectionId.ShouldEqual(_client.ConnectionId);
    [Fact] void should_use_the_event_types_the_standby_registered() => _subscription.EventTypes.ShouldContainOnly(event_type);
    [Fact] void should_update_the_definition_to_the_event_types_of_the_standby() => _definitionStorage.State.EventTypes.ShouldContainOnly(event_type);
    [Fact] void should_record_the_standby_as_the_target() => _stateStorage.State.DurableSubscription!.Targets.Select(target => target.ConnectionId).ShouldContainOnly(_client.ConnectionId.Value);
    [Fact] void should_have_no_standby_left() => (_stateStorage.State.DurableSubscription!.Standbys ?? []).ShouldBeEmpty();
}

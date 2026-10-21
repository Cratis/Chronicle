// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_activation_does_not_know_the_client : given.an_observer_that_lost_its_client
{
    ObserverSubscription _subscription;

    async Task Because()
    {
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_target_the_client() => _subscription.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_client.ConnectionId);
    [Fact] void should_observe_the_event_types_of_the_client() => _subscription.EventTypes.ShouldContainOnly(event_type);
    [Fact] async Task should_know_the_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeTrue();
}

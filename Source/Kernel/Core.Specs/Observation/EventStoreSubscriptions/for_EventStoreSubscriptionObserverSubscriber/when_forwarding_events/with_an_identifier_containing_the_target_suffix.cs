// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_an_identifier_containing_the_target_suffix : given.a_pinned_subscriber
{
    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns([
        _definition,
        new EventStoreSubscriptionDefinition("subscription->Lobby", "Admin", [new EventType("event-type-1", 2)]) { GenerationDelivery = EventGenerationDelivery.Pinned }]);
    async Task Because() => _result = await Forward();
    [Fact] void should_prefer_the_current_observer_identity_over_a_legacy_identifier_match() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_not_confuse_the_two_subscriptions() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}

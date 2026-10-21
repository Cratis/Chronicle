// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_the_target_generation_is_missing : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns([
        new EventStoreSubscriptionDefinition("subscription", "Admin", [new EventType("event-type-1", 2)])
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        }]);

    async Task Because() => _result = await Forward();

    [Fact] void should_fail_explicitly() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_append() => AppendCalls().ShouldEqual(0);
    [Fact] void should_not_acknowledge_the_source_event() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}

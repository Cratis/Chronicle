// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_target_schema_fallback : given.a_pinned_subscriber
{
    void Establish()
    {
        _definition = _definition with { EventTypes = [new EventType("event-type-1", 2)] };
        _storage.GetEventStore("Lobby").EventTypes.HasFor("event-type-1", 2).Returns(true);
        _storage.GetEventStore("Lobby").EventTypes.GetFor("event-type-1", 2).Returns(new Concepts.EventTypes.EventTypeSchema(new("event-type-1", 1), EventTypeOwner.Client, EventTypeSource.Code, _schema));
    }
    async Task Because() => _result = await Forward();
    [Fact] void should_reject_a_generation_absent_from_the_definition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_append_with_the_substitute_schema() => AppendCalls().ShouldEqual(0);
    [Fact] void should_not_advance() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}

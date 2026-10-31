// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_failed_authoritative_definition_read : given.a_pinned_subscriber
{
    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns(Task.FromException<IEnumerable<EventStoreSubscriptionDefinition>>(new IOException("Storage unavailable")));
    async Task Because() => _result = await Forward();
    [Fact] void should_fail_instead_of_downgrading_to_compatibility() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_append() => AppendCalls().ShouldEqual(0);
    [Fact] void should_not_acknowledge_the_event() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}

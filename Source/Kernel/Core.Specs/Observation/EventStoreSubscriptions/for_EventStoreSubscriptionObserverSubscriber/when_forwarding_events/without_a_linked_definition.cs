// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class without_a_linked_definition : given.a_pinned_subscriber
{
    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns([]);

    async Task Because() => _result = await Forward();

    [Fact] void should_preserve_the_existing_unlinked_delivery_behavior() => ForwardedType().ShouldEqual(new EventType("event-type-1", 2));
    [Fact] void should_not_infer_the_missing_pinned_policy() => AppendCalls().ShouldEqual(1);
    [Fact] void should_acknowledge_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}

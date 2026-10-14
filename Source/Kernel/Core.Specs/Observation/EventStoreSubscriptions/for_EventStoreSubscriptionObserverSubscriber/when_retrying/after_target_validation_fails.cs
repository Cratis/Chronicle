// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_retrying;

public class after_target_validation_fails : given.a_pinned_subscriber
{
    ObserverSubscriberResult _first;
    void Establish() => _storage.GetEventStore("Lobby").EventTypes.HasFor("event-type-1", 1).Returns(false, true);
    async Task Because()
    {
        _first = await Forward();
        _result = await Forward();
    }
    [Fact] void should_not_acknowledge_the_failed_attempt() => _first.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_succeed_on_retry() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_append_once() => AppendCalls().ShouldEqual(1);
    [Fact] void should_keep_the_same_pin() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}

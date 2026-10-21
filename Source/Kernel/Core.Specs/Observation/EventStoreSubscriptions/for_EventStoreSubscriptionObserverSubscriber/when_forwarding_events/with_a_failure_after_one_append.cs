// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_failure_after_one_append : given.a_pinned_subscriber
{
    void Establish()
    {
        _events = [_events[0], _events[0] with { Context = _events[0].Context with { SequenceNumber = 43UL } }];
        _storage.GetEventStore("Lobby").EventTypes.HasFor("event-type-1", 1).Returns(true, false);
    }
    async Task Because() => _result = await Forward();
    [Fact] void should_fail_the_batch() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_acknowledge_only_the_successful_prefix() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_append_only_the_prefix() => AppendCalls().ShouldEqual(1);
}

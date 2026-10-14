// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_retrying;

public class after_failure_with_a_successful_prefix : given.a_pinned_subscriber
{
    ObserverSubscriberResult _first;
    void Establish()
    {
        _events = [_events[0], _events[0] with { Context = _events[0].Context with { SequenceNumber = 43UL } }];
        _storage.GetEventStore("Lobby").EventTypes.HasFor("event-type-1", 1).Returns(true, false, true);
    }
    async Task Because()
    {
        _first = await Forward();
        _events = _events.Where(@event => @event.Context.SequenceNumber.Value > _first.LastSuccessfulObservation.Value).ToArray();
        _result = await Forward();
    }
    [Fact] void should_report_only_the_successful_prefix_on_failure() => _first.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_acknowledge_the_retried_event() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_append_the_acknowledged_prefix_twice() => AppendCalls().ShouldEqual(2);
    [Fact] void should_preserve_the_pin_on_retry() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}

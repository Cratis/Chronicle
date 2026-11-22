// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_preparing_a_mixed_run : given.a_partition_run
{
    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_observe_the_entire_run_tail() => _result.ObservedTail.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_find_the_subscription_before_applying_the_cursor() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_keep_events_at_and_above_the_cursor_in_order() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(11UL, 12UL);
    [Fact] void should_keep_unsubscribed_types_after_a_subscribed_event_in_the_run() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.EventType).ShouldEqual<EventType>(_otherType, _otherType);
    [Fact] void should_preserve_the_original_event_instances() => ReferenceEquals(_result.EventsToHandle[0], _events[1]).ShouldBeTrue();
    [Fact] void should_not_mutate_the_input() => _events.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(10UL, 11UL, 12UL);
}

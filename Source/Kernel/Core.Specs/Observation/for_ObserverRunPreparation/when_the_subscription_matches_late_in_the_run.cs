// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_the_subscription_matches_late_in_the_run : given.a_partition_run
{
    void Establish()
    {
        var first = Event(_otherType, 10);
        var second = Event(_otherType, 11);
        var last = Event(_subscribedType, 12);
        _events =
        [
            first with { Context = first.Context with { EventSourceType = "wanted-source", EventStreamType = "other-stream", Tags = [] } },
            second with { Context = second.Context with { EventSourceType = "other-source", EventStreamType = "wanted-stream", Tags = [] } },
            last with { Context = last.Context with { EventSourceType = "other-source", EventStreamType = "other-stream", Tags = [new("wanted")] } }
        ];
        _context = _context with { Filters = new(["wanted"], "wanted-source", "wanted-stream") };
    }

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_keep_independent_filter_matches_before_the_subscription_match() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_retain_cursor_eligible_events_before_and_after_the_match() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(11UL, 12UL);
    [Fact] void should_preserve_the_earlier_original_event() => ReferenceEquals(_result.EventsToHandle[0], _events[1]).ShouldBeTrue();
    [Fact] void should_observe_the_last_input() => _result.ObservedTail.ShouldEqual((EventSequenceNumber)12UL);
}

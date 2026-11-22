// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_filter_dimensions_match_different_events : given.a_partition_run
{
    void Establish()
    {
        _events =
        [
            _events[0] with { Context = _events[0].Context with { EventSourceType = "wanted-source", EventStreamType = "other-stream", Tags = [] } },
            _events[1] with { Context = _events[1].Context with { EventSourceType = "other-source", EventStreamType = "wanted-stream", Tags = [] } },
            _events[2] with { Context = _events[2].Context with { EventSourceType = "other-source", EventStreamType = "other-stream", Tags = [new("wanted")] } }
        ];
        _context = _context with { Filters = new(["wanted"], "wanted-source", "wanted-stream") };
    }

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_match_each_dimension_across_the_whole_run() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_allow_the_source_and_subscription_matches_below_the_cursor() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(11UL, 12UL);
    [Fact] void should_not_filter_delivered_events_individually() => ReferenceEquals(_result.EventsToHandle[0], _events[1]).ShouldBeTrue();
}

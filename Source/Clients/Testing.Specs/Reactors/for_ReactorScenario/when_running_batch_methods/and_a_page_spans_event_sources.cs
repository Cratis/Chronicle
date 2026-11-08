// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods;

public class and_a_page_spans_event_sources : given.a_seat_index_scenario
{
    EventSourceId _first;
    EventSourceId _second;

    void Establish()
    {
        _first = EventSourceId.New();
        _second = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given.Page(
        new(_first, new SeatBooked("1A")),
        new(_second, new SeatBooked("2B")),
        new(_first, new SeatReleased("1A")));

    [Fact] void should_call_the_batch_method_once() => _log.Batches.Count.ShouldEqual(1);
    [Fact] void should_keep_the_order_the_events_were_given_in() => _log.Batches[0].SequenceEqual([new SeatBooked("1A"), new SeatBooked("2B"), new SeatReleased("1A")]).ShouldBeTrue();
    [Fact] void should_name_both_partitions_in_the_context() => _log.Contexts[0].Partitions.ShouldContainOnly(_first, _second);
}

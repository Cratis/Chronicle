// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods;

public class and_one_call_gives_several_events : given.a_seat_index_scenario
{
    EventSourceId _id;

    void Establish() => _id = EventSourceId.New();

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new SeatBooked("1A"), new SeatNoted("window"), new SeatReleased("1A"));

    [Fact] void should_call_the_batch_method_once() => _log.Batches.Count.ShouldEqual(1);
    [Fact] void should_give_the_batch_method_the_events_it_observes_in_order() => _log.Batches[0].ShouldContainOnly(new SeatBooked("1A"), new SeatReleased("1A"));
    [Fact] void should_give_the_batch_events_in_sequence_order() => _log.Batches[0][0].ShouldEqual(new SeatBooked("1A"));
    [Fact] void should_deliver_the_page_as_live() => _log.Contexts[0].Origin.ShouldEqual(ObservationOrigin.Live);
    [Fact] void should_describe_the_whole_page() => _log.Contexts[0].EventCount.ShouldEqual(3);
    [Fact] void should_give_the_per_event_method_only_the_other_event() => _log.PerEvent.ShouldContainOnly(new SeatNoted("window"));
    [Fact] void should_report_no_partition_outcomes() => _scenario.PartitionOutcomes.ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_batch_reducers;

public class and_events_are_reduced : given.a_batch_reducer_scenario
{
    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new SeatCounted(), new SeatCounted(), new SeatUncounted());

    [Fact] void should_fold_the_events() => _scenario.Instance!.Seats.ShouldEqual(1);
    [Fact] void should_call_the_batch_method_once_for_the_event_source() => _scenario.Instance!.Calls.ShouldEqual(1);
    [Fact] void should_deliver_the_events_as_live() => _scenario.Instance!.Origin.ShouldEqual(ObservationOrigin.Live);
    [Fact] void should_describe_the_page() => _scenario.Instance!.EventCount.ShouldEqual(3);
}

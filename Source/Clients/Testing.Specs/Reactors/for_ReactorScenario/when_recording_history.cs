// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_recording_history : a_route_scenario<RouteRecordingReactor>
{
    RecordedOccurrence _single;
    IReadOnlyList<RecordedOccurrence> _several;
    readonly ReactorRouteRecorded _event = new("first");

    async Task Because()
    {
        _single = await _scenario.Given.ForEventSource(_source).History(_event);
        _several = await _scenario.Given.ForEventSource(_source).History(new ReactorRouteRecorded("second"), new ReactorRouteRecorded("third"));
    }

    [Fact] void should_not_invoke_the_reactor() => _recorder.DidNotReceiveWithAnyArgs().Record(default!, default!);
    [Fact] void should_not_produce_side_effects() => _scenario.Produced.ShouldBeEmpty();
    [Fact] void should_record_the_original_event() => ReferenceEquals(_single.Event, _event).ShouldBeTrue();
    [Fact] void should_start_at_the_first_sequence_number() => _single.Context.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_number_history_across_calls() => _several.Select(occurrence => occurrence.Context.SequenceNumber).ShouldEqual([EventSequenceNumber.First + 1, EventSequenceNumber.First + 2]);
    [Fact] void should_allocate_a_distinct_correlation_for_each_occurrence() => _several[0].Context.CorrelationId.ShouldNotEqual(_single.Context.CorrelationId);
    [Fact] void should_record_all_given_events() => _several.Count.ShouldEqual(2);
}

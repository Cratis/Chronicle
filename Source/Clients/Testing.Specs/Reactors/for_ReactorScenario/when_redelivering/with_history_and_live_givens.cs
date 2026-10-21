// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_history_and_live_givens : a_recorded_occurrence
{
    RecordedOccurrence _next;
    readonly ReactorRouteRecorded _liveEvent = new("live");

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        await _scenario.Given.ForEventSource(_source).Events(_liveEvent);
        await _scenario.Redeliver(_occurrence);
        _next = await _scenario.Given.ForEventSource(_source).History(new ReactorRouteRecorded("next"));
    }

    [Fact] void should_number_live_givens_after_history() => _occurrenceRecorder.Received().Record(_liveEvent, Arg.Is<EventContext>(context => context.SequenceNumber == EventSequenceNumber.First + 1), Arg.Any<ReactorDelivery>());
    [Fact] void should_not_advance_the_sequence_for_redelivery() => _next.Context.SequenceNumber.ShouldEqual(EventSequenceNumber.First + 2);
    [Fact] void should_use_event_log_default_source_type() => _next.Context.EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_use_event_log_default_stream_type() => _next.Context.EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_leave_the_original_builder_route_unchanged() => _next.Context.EventStreamId.ShouldEqual(new EventStreamId(EventStreamId.Default));
}

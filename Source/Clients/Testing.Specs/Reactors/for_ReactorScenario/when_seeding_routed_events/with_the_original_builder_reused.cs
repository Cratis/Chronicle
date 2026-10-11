// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_the_original_builder_reused : a_route_scenario<RouteRecordingReactor>
{
    ReactorSourceGivenBuilder<RouteRecordingReactor> _original;
    ReactorSourceGivenBuilder<RouteRecordingReactor> _routed;

    void Establish() => _original = _scenario.Given.ForEventSource(_source);

    async Task Because()
    {
        _routed = _original.OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly");
        await _routed.Events(new ReactorRouteRecorded("routed"));
        await _original.Events(new ReactorRouteRecorded("original"));
    }

    [Fact] void should_create_a_separate_builder() => ReferenceEquals(_original, _routed).ShouldBeFalse();
    [Fact] void should_preserve_the_original_source_type() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "original"), Arg.Is<EventContext>(context => context.EventSourceType == EventSourceType.Default));
    [Fact] void should_preserve_the_original_stream_type() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "original"), Arg.Is<EventContext>(context => context.EventStreamType == EventStreamType.All));
    [Fact] void should_preserve_the_original_stream_id() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "original"), Arg.Is<EventContext>(context => context.EventStreamId == new EventStreamId(EventStreamId.Default)));
}

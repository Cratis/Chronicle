// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_a_typed_source_and_stream_filter : a_route_scenario<TypedFilteredRouteReactor>
{
    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new ReactorRouteRecorded("wrong source"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Shipping")
            .Events(new ReactorRouteRecorded("wrong stream"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly")
            .Events(new ReactorRouteRecorded("first"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "yearly")
            .Events(new ReactorRouteRecorded("second"));
    }

    [Fact] void should_ignore_a_nonmatching_source() => _recorder.DidNotReceive().Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "wrong source"), Arg.Any<EventContext>());
    [Fact] void should_ignore_a_nonmatching_stream() => _recorder.DidNotReceive().Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "wrong stream"), Arg.Any<EventContext>());
    [Fact] void should_handle_both_matching_routes_regardless_of_stream_id() => _recorder.Received(2).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Any<EventContext>());
    [Fact] void should_preserve_the_input_position_after_skips() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "second"), Arg.Is<EventContext>(context => context.SequenceNumber == EventSequenceNumber.First + 3));
}

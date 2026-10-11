// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_a_route : a_route_scenario<RouteRecordingReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly")
        .Events(new ReactorRouteRecorded("routed"));

    [Fact] void should_deliver_the_event_source_id() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventSourceId == _source));
    [Fact] void should_deliver_the_event_source_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventSourceType == new EventSourceType("RouteOrder")));
    [Fact] void should_deliver_the_event_stream_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamType == new EventStreamType("Payments")));
    [Fact] void should_deliver_the_event_stream_id() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamId == new EventStreamId("monthly")));
}

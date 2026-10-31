// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_only_a_stream_type : a_route_scenario<RouteRecordingReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventStreamType: "Payments")
        .Events(new ReactorRouteRecorded("stream"));

    [Fact] void should_use_the_default_source_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventSourceType == EventSourceType.Default));
    [Fact] void should_deliver_the_stream_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamType == new EventStreamType("Payments")));
    [Fact] void should_use_the_default_stream_id() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamId == new EventStreamId(EventStreamId.Default)));
}

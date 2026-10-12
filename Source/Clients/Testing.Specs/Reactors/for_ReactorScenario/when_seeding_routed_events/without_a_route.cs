// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class without_a_route : a_route_scenario<RouteRecordingReactor>
{
    async Task Because() => await _scenario.Given.ForEventSourceId(_source).Events(new ReactorRouteRecorded("unrouted"));

    [Fact] void should_keep_the_default_source_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventSourceType == EventSourceType.Default));
    [Fact] void should_keep_the_default_stream_type() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamType == EventStreamType.All));
    [Fact] void should_keep_the_default_stream_id() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.EventStreamId == new EventStreamId(EventStreamId.Default)));
}

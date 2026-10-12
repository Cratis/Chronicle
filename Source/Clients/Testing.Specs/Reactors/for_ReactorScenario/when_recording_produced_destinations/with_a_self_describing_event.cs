// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_a_self_describing_event : a_route_scenario<WrappedRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "input-stream")
        .Events(new ReactorRouteRecorded("wrapped"));

    [Fact] void should_unwrap_the_produced_event() => _scenario.ProducedEvents.Single().Event.ShouldEqual(new ProducedRouteRecorded("wrapped"));
    [Fact] void should_keep_the_raw_produced_list_unwrapped() => _scenario.Produced.ShouldContainOnly(new ProducedRouteRecorded("wrapped"));
    [Fact] void should_use_the_wrapper_source_id_instead_of_the_provider() => _scenario.ProducedEvents.Single().EventSourceId.ShouldEqual(new EventSourceId("member"));
    [Fact] void should_use_the_wrapper_source_type() => _scenario.ProducedEvents.Single().EventSourceType.ShouldEqual(new EventSourceType("Member"));
    [Fact] void should_use_the_wrapper_stream_type() => _scenario.ProducedEvents.Single().EventStreamType.ShouldEqual(new EventStreamType("Activity"));
    [Fact] void should_use_the_wrapper_stream_id() => _scenario.ProducedEvents.Single().EventStreamId.ShouldEqual(new EventStreamId("wrapper-stream"));
}

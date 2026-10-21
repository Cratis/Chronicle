// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_a_bare_event : a_route_scenario<BareEventRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "input-stream")
        .Events(new ReactorRouteRecorded("bare"));

    [Fact] void should_record_the_event() => _scenario.ProducedEvents.Single().Event.ShouldEqual(new ProducedRouteRecorded("bare"));
    [Fact] void should_keep_the_raw_produced_list() => _scenario.Produced.ShouldContainOnly(new ProducedRouteRecorded("bare"));
    [Fact] void should_target_the_triggering_source_id() => _scenario.ProducedEvents.Single().EventSourceId.ShouldEqual(_source);
    [Fact] void should_use_the_default_source_type_instead_of_the_input_route() => _scenario.ProducedEvents.Single().EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_use_the_default_stream_type_instead_of_the_input_route() => _scenario.ProducedEvents.Single().EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_use_the_default_stream_id_instead_of_the_input_route() => _scenario.ProducedEvents.Single().EventStreamId.ShouldEqual(new EventStreamId(EventStreamId.Default));
}

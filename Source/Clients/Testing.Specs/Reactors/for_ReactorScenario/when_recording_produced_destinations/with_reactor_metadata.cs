// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_reactor_metadata : a_route_scenario<ProvidedDestinationRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "input-stream")
        .Events(new ReactorRouteRecorded("metadata"));

    [Fact] void should_use_the_event_source_id_provider() => _scenario.ProducedEvents.Single().EventSourceId.ShouldEqual(new EventSourceId("destination"));
    [Fact] void should_use_the_source_type_attribute() => _scenario.ProducedEvents.Single().EventSourceType.ShouldEqual(new EventSourceType("RouteOrder"));
    [Fact] void should_use_the_stream_type_attribute() => _scenario.ProducedEvents.Single().EventStreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_use_the_stream_id_attribute_instead_of_the_input_stream_id() => _scenario.ProducedEvents.Single().EventStreamId.ShouldEqual(new EventStreamId("produced-stream"));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_a_reactor_providing_the_stream_id : a_route_scenario<ProvidedStreamIdRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "input-stream")
        .Events(new ReactorRouteRecorded("provided"));

    [Fact] void should_record_the_bare_event() => _scenario.ProducedEvents.Count.ShouldEqual(1);
    [Fact] void should_use_the_provided_stream_id_instead_of_the_input_stream_id() => _scenario.ProducedEvents[0].EventStreamId.ShouldEqual(new EventStreamId("provided-stream"));
    [Fact] void should_preserve_the_event() => _scenario.ProducedEvents[0].Event.ShouldEqual(new ProducedRouteRecorded("provided"));
}

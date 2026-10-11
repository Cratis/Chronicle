// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_a_typed_destination : a_route_scenario<TypedProducedRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("typed"));

    [Fact] void should_use_the_explicit_source_id() => _scenario.ProducedEvents.Single().EventSourceId.ShouldEqual(new EventSourceId("order"));
    [Fact] void should_resolve_the_source_type_from_the_definition() => _scenario.ProducedEvents.Single().EventSourceType.ShouldEqual(new EventSourceType("RouteOrder"));
    [Fact] void should_resolve_the_stream_type_from_the_definition() => _scenario.ProducedEvents.Single().EventStreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_preserve_the_explicit_stream_id() => _scenario.ProducedEvents.Single().EventStreamId.ShouldEqual(new EventStreamId("typed-stream"));
}

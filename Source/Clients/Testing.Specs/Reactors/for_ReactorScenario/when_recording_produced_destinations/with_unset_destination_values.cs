// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_unset_destination_values : a_route_scenario<UnsetProducedRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("unset"));

    [Fact] void should_apply_the_event_logs_source_type_default() => _scenario.ProducedEvents.Single().EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_apply_the_event_logs_stream_type_default() => _scenario.ProducedEvents.Single().EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_apply_the_event_logs_stream_id_default() => _scenario.ProducedEvents.Single().EventStreamId.ShouldEqual(new EventStreamId(EventStreamId.Default));
}

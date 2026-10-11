// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_a_bare_event_collection : a_route_scenario<BareEventCollectionRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("first"));

    [Fact] void should_preserve_event_order_and_destinations() => _scenario.ProducedEvents.ShouldContainOnly(
        new ProducedEvent(new ProducedRouteRecorded("first"), _source, EventSourceType.Default, EventStreamType.All, EventStreamId.Default),
        new ProducedEvent(new ProducedRouteRecorded("second"), _source, EventSourceType.Default, EventStreamType.All, EventStreamId.Default));
    [Fact] void should_keep_all_raw_events() => _scenario.Produced.ShouldContainOnly(new ProducedRouteRecorded("first"), new ProducedRouteRecorded("second"));
}

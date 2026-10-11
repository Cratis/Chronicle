// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations;

public class with_events_for_several_event_sources : a_route_scenario<SeveralEventSourcesRouteReactor>
{
    async Task Because() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "input-stream")
        .Events(new ReactorRouteRecorded("first"));

    [Fact] void should_record_both_events() => _scenario.ProducedEvents.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_first_event_route() => _scenario.ProducedEvents[0].ShouldEqual(new ProducedEvent(new ProducedRouteRecorded("first"), "member", "Member", "Activity", "member-stream"));
    [Fact] void should_preserve_the_second_event_route() => _scenario.ProducedEvents[1].ShouldEqual(new ProducedEvent(new ProducedRouteRecorded("second"), "account", "Account", "Transactions", "account-stream"));
}

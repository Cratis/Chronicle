// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_destinations;

public class with_a_matching_destination_predicate : a_route_scenario<WrappedRouteReactor>
{
    Exception _exception;

    async Task Establish() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments")
        .Events(new ReactorRouteRecorded("wrapped"));

    void Because() => _exception = Catch.Exception(() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>((produced, @event) =>
        produced.EventSourceId == new EventSourceId("member") &&
        produced.EventSourceType == new EventSourceType("Member") &&
        produced.EventStreamType == new EventStreamType("Activity") &&
        produced.EventStreamId == new EventStreamId("wrapper-stream") &&
        @event.Note == "wrapped"));

    [Fact] void should_accept_the_matching_event_and_destination() => _exception.ShouldBeNull();
}

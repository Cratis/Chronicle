// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_destinations;

public class with_a_different_stream : a_route_scenario<WrappedRouteReactor>
{
    Exception _exception;

    async Task Establish() => await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments")
        .Events(new ReactorRouteRecorded("wrapped"));

    void Because() => _exception = Catch.Exception(() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>((produced, @event) =>
        produced.EventStreamType == new EventStreamType("Shipping") && @event.Note == "wrapped"));

    [Fact] void should_fail_the_assertion() => _exception.ShouldBeOfExactType<ReactorSideEffectAssertionException>();
    [Fact] void should_list_the_actual_destination() => _exception.Message.Contains("source id 'member', source type 'Member', stream type 'Activity', stream id 'wrapper-stream'", StringComparison.Ordinal).ShouldBeTrue();
}

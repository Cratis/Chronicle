// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_a_stream_filtered_reactor : a_route_scenario<StreamFilteredRouteReactor>
{
    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Shipping")
            .Events(new ReactorRouteRecorded("off-route"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new ReactorRouteRecorded("on-route"));
    }

    [Fact] void should_ignore_the_off_route_event() => _recorder.DidNotReceive().Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "off-route"), Arg.Any<EventContext>());
    [Fact] void should_handle_the_matching_stream_on_any_source() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "on-route"), Arg.Any<EventContext>());
}

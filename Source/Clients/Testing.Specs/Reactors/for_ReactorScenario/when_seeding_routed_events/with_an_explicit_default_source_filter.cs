// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_an_explicit_default_source_filter : a_route_scenario<DefaultSourceRouteReactor>
{
    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder")
            .Events(new ReactorRouteRecorded("off-route"));
        await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("default"));
    }

    [Fact] void should_not_treat_default_as_a_wildcard() => _recorder.DidNotReceive().Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "off-route"), Arg.Any<EventContext>());
    [Fact] void should_handle_the_literal_default_source() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "default"), Arg.Any<EventContext>());
}

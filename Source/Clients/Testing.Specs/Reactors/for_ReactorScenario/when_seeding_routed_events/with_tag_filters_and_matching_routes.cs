// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_tag_filters_and_matching_routes : a_route_scenario<with_tag_filters_and_matching_routes.TagFilteredReactor>
{
    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).OnStream("Other").Events(new ReactorRouteRecorded("off-route"));
        await _scenario.Given.ForEventSource(_source).OnStream("RouteOrder").Events(new ReactorRouteRecorded("on-route"));
    }

    [Fact] void should_preserve_existing_route_selection_without_adding_reactor_tag_filtering() => _recorder.Received(1).Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "on-route"), Arg.Any<EventContext>());
    [Fact] void should_still_exclude_the_wrong_route() => _recorder.DidNotReceive().Record(Arg.Is<ReactorRouteRecorded>(@event => @event.Note == "off-route"), Arg.Any<EventContext>());

    [FilterEventsByTag("required"), EventSourceType("RouteOrder")]
    public class TagFilteredReactor(IRecordEventRoute recorder) : IReactor
    {
        public void On(ReactorRouteRecorded @event, EventContext context) => recorder.Record(@event, context);
    }
}

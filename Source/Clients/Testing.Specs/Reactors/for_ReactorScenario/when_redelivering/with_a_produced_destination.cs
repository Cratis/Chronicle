// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_a_produced_destination : a_route_scenario<WrappedRouteReactor>
{
    RecordedOccurrence _occurrence;

    async Task Establish() => _occurrence = await _scenario.Given.ForEventSource(_source)
        .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments").History(new ReactorRouteRecorded("recorded"));

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_occurrence);
    }

    [Fact] void should_record_the_returned_event() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>(@event => @event.Note == "recorded");
    [Fact] void should_preserve_the_explicit_destination() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>((destination, _) => destination.EventSourceId.Value == "member" && destination.EventSourceType.Value == "Member" && destination.EventStreamType.Value == "Activity" && destination.EventStreamId.Value == "wrapper-stream");
    [Fact] void should_accumulate_produced_events_across_deliveries() => _scenario.ProducedEvents.Count.ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_a_stream_filter : a_route_scenario<StreamFilteredRouteReactor>
{
    RecordedOccurrence _matching;
    RecordedOccurrence _filtered;

    async Task Establish()
    {
        _filtered = await _scenario.Given.ForEventSource(_source).OnStream(eventStreamType: "Orders").History(new ReactorRouteRecorded("filtered"));
        _matching = await _scenario.Given.ForEventSource(_source).OnStream(eventStreamType: "Payments", eventStreamId: "monthly").History(new ReactorRouteRecorded("matching"));
    }

    async Task Because()
    {
        await _scenario.Redeliver(_filtered);
        await _scenario.Redeliver(_matching);
    }

    [Fact] void should_not_invoke_the_handler_for_the_filtered_occurrence() => _recorder.DidNotReceive().Record((ReactorRouteRecorded)_filtered.Event, _filtered.Context);
    [Fact] void should_deliver_the_matching_occurrence_with_its_original_route() => _recorder.Received(1).Record((ReactorRouteRecorded)_matching.Event, _matching.Context);
}

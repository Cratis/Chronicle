// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

public class a_recorded_occurrence : a_route_scenario<OccurrenceRecordingReactor>
{
    protected IRecordOccurrence _occurrenceRecorder;
    protected RecordedOccurrence _occurrence;
    protected readonly ReactorRouteRecorded _event = new("recorded");

    async Task Establish()
    {
        _occurrenceRecorder = Substitute.For<IRecordOccurrence>();
        _scenario.Services.AddSingleton(_occurrenceRecorder);
        _occurrence = await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly")
            .History(_event);
    }
}

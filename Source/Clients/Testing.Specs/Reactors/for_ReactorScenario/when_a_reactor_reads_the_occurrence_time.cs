// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_reads_the_occurrence_time : a_route_scenario<RouteRecordingReactor>
{
    async Task Because() => await _scenario.Given.ForEventSourceId(_source).Events(new ReactorRouteRecorded("observed"));

    [Fact] void should_deliver_occurred_in_utc() => _recorder.Received(1).Record(Arg.Any<ReactorRouteRecorded>(), Arg.Is<EventContext>(context => context.Occurred.Offset == TimeSpan.Zero));
}

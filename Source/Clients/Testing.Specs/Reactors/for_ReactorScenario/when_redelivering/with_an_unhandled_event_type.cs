// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_an_unhandled_event_type : a_route_scenario<RouteRecordingReactor>
{
    RecordedOccurrence _occurrence;

    async Task Establish() => _occurrence = await _scenario.Given.ForEventSource(_source).History(new ProducedRouteRecorded("unhandled"));

    async Task Because() => await _scenario.Redeliver(_occurrence);

    [Fact] void should_not_invoke_the_handler() => _recorder.DidNotReceiveWithAnyArgs().Record(default!, default!);
    [Fact] void should_not_produce_side_effects() => _scenario.Produced.ShouldBeEmpty();
}

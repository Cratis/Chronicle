// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_destinations;

public class with_a_matching_source_id : a_route_scenario<BareEventRouteReactor>
{
    Exception _exception;

    async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("bare"));

    void Because() => _exception = Catch.Exception(() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>(_source));

    [Fact] void should_accept_the_matching_destination() => _exception.ShouldBeNull();
}

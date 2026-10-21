// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_destinations;

public class without_produced_events : a_route_scenario<BareEventRouteReactor>
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>(_source));

    [Fact] void should_fail_the_assertion() => _exception.ShouldBeOfExactType<ReactorSideEffectAssertionException>();
    [Fact] void should_report_that_no_destinations_were_recorded() => _exception.Message.Contains("recorded destinations were: nothing", StringComparison.Ordinal).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_destinations;

public class with_a_different_source_id : a_route_scenario<BareEventRouteReactor>
{
    Exception _exception;

    async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("bare"));

    void Because() => _exception = Catch.Exception(() => _scenario.ShouldHaveProduced<ProducedRouteRecorded>(new EventSourceId("other")));

    [Fact] void should_fail_the_assertion() => _exception.ShouldBeOfExactType<ReactorSideEffectAssertionException>();
    [Fact] void should_name_the_expected_source() => _exception.Message.Contains("for event source 'other'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_actual_source() => _exception.Message.Contains($"source id '{_source.Value}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_actual_source_type() => _exception.Message.Contains("source type 'Default'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_actual_stream_type() => _exception.Message.Contains($"stream type '{EventStreamType.All.Value}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_actual_stream_id() => _exception.Message.Contains("stream id 'Default'", StringComparison.Ordinal).ShouldBeTrue();
}

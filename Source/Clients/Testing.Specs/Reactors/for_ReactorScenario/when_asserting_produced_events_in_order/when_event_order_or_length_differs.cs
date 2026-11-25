// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_events_in_order;

public class when_event_order_or_length_differs : a_reaction_slice<BareEventRouteReactor>
{
    Exception _ordered;
    Exception _reversed;
    Exception _extra;
    Exception _missing;

    async Task Establish()
    {
        await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("first"));
        await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("second"));
    }

    void Because()
    {
        _ordered = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInOrder(new ProducedRouteRecorded("first"), new ProducedRouteRecorded("second")));
        _reversed = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInOrder(new ProducedRouteRecorded("second"), new ProducedRouteRecorded("first")));
        _extra = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInOrder(new ProducedRouteRecorded("first")));
        _missing = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInOrder(new ProducedRouteRecorded("first"), new ProducedRouteRecorded("second"), new ProducedRouteRecorded("third")));
    }

    [Fact] void should_accept_the_complete_cumulative_ordered_list() => _ordered.ShouldBeNull();
    [Fact] void should_report_the_first_zero_based_index() => _reversed.Message.ShouldContain("index 0");
    [Fact] void should_report_the_payload_path_and_values()
    {
        _reversed.Message.ShouldContain("$.Note");
        _reversed.Message.ShouldContain("first");
        _reversed.Message.ShouldContain("second");
    }
    [Fact] void should_reject_extra_recorded_events() => _extra.Message.ShouldContain("index 1");
    [Fact] void should_reject_missing_recorded_events() => _missing.Message.ShouldContain("index 2");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_events_in_any_order;

public class when_matching_recorded_event_multisets : a_reaction_slice<AssertionReactor>
{
    Exception _error;

    async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("first"));

    void Because() => _error = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(
        AssertionReactor.Create("second"), AssertionReactor.Create("first"), AssertionReactor.Create("first")));

    [Fact] void should_ignore_event_order_and_compare_nested_contents() => _error.ShouldBeNull();
    [Fact] void should_record_both_duplicate_occurrences() => _scenario.ProducedEvents.Count.ShouldEqual(3);
}

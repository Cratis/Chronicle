// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_events_in_any_order;

public class when_recorded_event_multisets_differ : a_reaction_slice<AssertionReactor>
{
    Exception _duplicate;
    Exception _content;
    Exception _type;
    Exception _empty;
    Exception _missing;

    async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("first"));

    void Because()
    {
        _duplicate = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(AssertionReactor.Create("first"), AssertionReactor.Create("second"), AssertionReactor.Create("second")));
        _content = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(AssertionReactor.Create("first"), AssertionReactor.Create("first"), AssertionReactor.Create("second") with { Values = [2, 1] }));
        _type = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(new { Name = "first", Values = new[] { 1, 2 }, Metadata = new Dictionary<string, object?>() }));
        _empty = Catch.Exception(_scenario.ShouldNotHaveProducedEvents);
        _missing = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(AssertionReactor.Create("first"), AssertionReactor.Create("first"), AssertionReactor.Create("second"), AssertionReactor.Create("extra")));
    }

    [Fact] void should_preserve_duplicate_counts() => _duplicate.ShouldBeOfExactType<ReactorSideEffectAssertionException>();
    [Fact] void should_report_missing_and_unexpected_occurrences()
    {
        _duplicate.Message.ShouldContain("Missing (1)");
        _duplicate.Message.ShouldContain("Unexpected (1)");
    }
    [Fact] void should_report_nested_collection_differences() => _content.Message.ShouldContain("$.Values[0]");
    [Fact] void should_require_the_event_runtime_type() => _type.Message.ShouldContain(".Type");
    [Fact] void should_reject_recorded_events_when_none_are_expected() => _empty.ShouldBeOfExactType<ReactorSideEffectAssertionException>();
    [Fact] void should_report_missing_events()
    {
        _missing.Message.ShouldContain("Missing (1)");
        _missing.Message.ShouldContain("extra");
    }
}

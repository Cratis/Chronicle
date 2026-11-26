// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_events_in_any_order;

public class when_nested_container_types_differ : a_reaction_slice<ContainerAssertionReactor>
{
    Exception _ordered;
    Exception _unordered;

    async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new ReactorRouteRecorded("first"));

    void Because()
    {
        var entries = new Dictionary<string, object?> { ["sequence"] = new[] { 2, 4 } };
        var frozen = new AssertionEvent("first", new[] { 1, 2 }, entries.ToFrozenDictionary());
        var readOnly = new AssertionEvent("first", new[] { 1, 2 }, new ReadOnlyDictionary<string, object?>(entries));
        _ordered = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInOrder(frozen));
        _unordered = Catch.Exception(() => _scenario.ShouldHaveProducedEventsInAnyOrder(readOnly));
    }

    [Fact] void should_compare_lists_arrays_iterators_and_frozen_dictionaries_by_contents_in_order() => _ordered.ShouldBeNull();
    [Fact] void should_compare_read_only_dictionaries_and_nested_iterators_by_contents_in_any_order() => _unordered.ShouldBeNull();
}

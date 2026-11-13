// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

public abstract class and_several_parents_hold_it<THarness> : given.a_sink_with_parents_holding_children<THarness>
    where THarness : ISinkHarness, new()
{
    IEnumerable<string> _first;
    IEnumerable<string> _second;
    IEnumerable<string> _unrelated;

    async Task Establish() => await SeedParents();

    async Task Because()
    {
        _keys = await _sink.FindKeysHoldingChild(Removal());
        _first = await ChildIdsOf("first");
        _second = await ChildIdsOf("second");
        _unrelated = await ChildIdsOf("unrelated");
    }

    [Fact] public void should_find_every_parent_holding_the_child() => KeyValues().ShouldEqual<IEnumerable<string>>(["first", "second"]);
    [Fact] public void should_not_remove_the_child_from_the_first_parent() => _first.ShouldEqual<IEnumerable<string>>([RemovedChild, "kept-on-first"]);
    [Fact] public void should_not_remove_the_child_from_the_second_parent() => _second.ShouldEqual<IEnumerable<string>>([RemovedChild]);
    [Fact] public void should_not_touch_a_parent_without_the_child() => _unrelated.ShouldEqual<IEnumerable<string>>(["other-child"]);
}

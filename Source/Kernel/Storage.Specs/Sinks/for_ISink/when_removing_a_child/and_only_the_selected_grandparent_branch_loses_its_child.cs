// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child;

/// <summary>
/// Two groups hold a parent with the same identifier and a child with the same key. The ancestor indexers select one
/// group and one parent, so only that branch loses the child.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_only_the_selected_grandparent_branch_loses_its_child<THarness> : given.a_read_model_with_nested_children<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Establish() => await Seed(
        "groups",
        Item("first", ("parents", new List<object> { Item("parent", ("children", new List<object> { Item("child"), Item("other") })) })),
        Item("second", ("parents", new List<object> { Item("parent", ("children", new List<object> { Item("child") })) })));

    Task Because() => RemoveChild(
        "[groups].[parents].[children]",
        "child",
        Indexer("[groups]", "first"),
        Indexer("[groups].[parents]", "parent"),
        Indexer("[groups].[parents].[children]", "child"));

    [Fact] public void should_keep_both_groups() => Ids(_stored["groups"]).ShouldEqual<IEnumerable<string>>(["first", "second"]);
    [Fact] public void should_remove_the_child_from_the_selected_branch() => Ids(Find(Find(_stored["groups"], "first")["parents"], "parent")["children"]).ShouldEqual<IEnumerable<string>>(["other"]);
    [Fact] public void should_keep_the_same_keyed_child_of_the_other_branch() => Ids(Find(Find(_stored["groups"], "second")["parents"], "parent")["children"]).ShouldEqual<IEnumerable<string>>(["child"]);
}

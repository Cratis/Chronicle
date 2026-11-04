// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child;

/// <summary>
/// A nested removal names the child collection of the selected parent. A parent whose own identifier equals the child
/// key is not a child of that collection and must survive, along with the same-keyed child of a sibling parent.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_parent_id_equals_the_child_key<THarness> : given.a_read_model_with_nested_children<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Establish() => await Seed(
        "parents",
        Item("shared", ("children", new List<object> { Item("shared"), Item("other") })),
        Item("sibling", ("children", new List<object> { Item("shared") })));

    Task Because() => RemoveChild("[parents].[children]", "shared", Indexer("[parents]", "shared"), Indexer("[parents].[children]", "shared"));

    [Fact] public void should_keep_both_parents() => Ids(_stored["parents"]).ShouldEqual<IEnumerable<string>>(["shared", "sibling"]);
    [Fact] public void should_remove_the_child_from_the_selected_parent() => Ids(Find(_stored["parents"], "shared")["children"]).ShouldEqual<IEnumerable<string>>(["other"]);
    [Fact] public void should_keep_the_same_keyed_child_of_the_other_parent() => Ids(Find(_stored["parents"], "sibling")["children"]).ShouldEqual<IEnumerable<string>>(["shared"]);
}

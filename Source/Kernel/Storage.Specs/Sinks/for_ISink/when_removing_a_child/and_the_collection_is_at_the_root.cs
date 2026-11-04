// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child;

/// <summary>
/// A removal from a collection directly on the read model removes only the keyed child.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_collection_is_at_the_root<THarness> : given.a_read_model_with_nested_children<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Establish() => await Seed("children", Item("removed"), Item("kept"));

    Task Because() => RemoveChild("[children]", "removed", Indexer("[children]", "removed"));

    [Fact] public void should_remove_only_the_keyed_child() => Ids(_stored["children"]).ShouldEqual<IEnumerable<string>>(["kept"]);
}

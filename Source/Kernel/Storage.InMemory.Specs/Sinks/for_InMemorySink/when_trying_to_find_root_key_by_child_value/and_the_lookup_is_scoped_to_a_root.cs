// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using DomainChanges = Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_trying_to_find_root_key_by_child_value;

public class and_the_lookup_is_scoped_to_a_root : given.an_in_memory_sink
{
    readonly Guid _firstRoot = Guid.NewGuid();
    readonly Guid _secondRoot = Guid.NewGuid();
    Monads.Option<Key> _matched;
    bool _missingRoot;
    bool _missingChild;

    async Task Establish()
    {
        await AddChild(_firstRoot, "shared");
        await AddChild(_firstRoot, "only-first");
        await AddChild(_secondRoot, "shared");
    }

    async Task Because()
    {
        _matched = await _sink.TryFindRootKeyByChildValueInRoot("children.childId", "shared", Root(_secondRoot));
        _missingRoot = (await _sink.TryFindRootKeyByChildValueInRoot("children.childId", "shared", Root(Guid.NewGuid()))).HasValue;
        _missingChild = (await _sink.TryFindRootKeyByChildValueInRoot("children.childId", "only-first", Root(_secondRoot))).HasValue;
    }

    Task AddChild(Guid root, string child) => _sink.ApplyChanges(
        new Key(root, ArrayIndexers.NoIndexers),
        new DomainChanges.Changeset([new DomainChanges.ChildAdded("children", new Key(child, ArrayIndexers.NoIndexers), new Dictionary<PropertyPath, object> { ["childId"] = child })]));

    static Key Root(Guid id) => new(id.ToString(), ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_the_requested_root_with_a_repeated_child_key() => _matched.Value.Value.ShouldEqual(_secondRoot);
    [Fact] void should_not_find_a_child_in_a_missing_root() => _missingRoot.ShouldBeFalse();
    [Fact] void should_not_select_a_child_from_another_root() => _missingChild.ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

using DomainChanges = Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_trying_to_find_root_key_by_child_value;

public class and_child_is_in_single_level_array : given.an_in_memory_sink
{
    Guid _rootKey;
    Guid _childKey;
    Monads.Option<Key> _result;

    async Task Establish()
    {
        _rootKey = Guid.NewGuid();
        _childKey = Guid.NewGuid();

        // Insert a document with a child in a single-level array
        await _sink.ApplyChanges(new Key(_rootKey, ArrayIndexers.NoIndexers), new DomainChanges.Changeset(
            [
                new DomainChanges.PropertiesChanged<object>(new Dictionary<PropertyPath, object>
                {
                    [new PropertyPath("name")] = "Root Document"
                }),
                new DomainChanges.ChildAdded(
                    new PropertyPath("children"),
                    new Key(_childKey, ArrayIndexers.NoIndexers),
                    new Dictionary<PropertyPath, object>
                    {
                        [new PropertyPath("childId")] = _childKey,
                        [new PropertyPath("name")] = "Child 1"
                    })
            ]));
    }

    async Task Because() => _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), _childKey);

    [Fact] void should_return_a_value() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_return_the_root_key() => _result.Value.Value.ShouldEqual(_rootKey);
}

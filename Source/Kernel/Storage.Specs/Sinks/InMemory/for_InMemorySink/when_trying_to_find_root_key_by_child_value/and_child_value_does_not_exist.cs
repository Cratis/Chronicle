// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.InMemory.for_InMemorySink.when_trying_to_find_root_key_by_child_value;

public class and_child_value_does_not_exist : given.an_in_memory_sink
{
    Guid _rootKey;
    Guid _nonExistentId;
    Monads.Option<Key> _result;

    async Task Establish()
    {
        _rootKey = Guid.NewGuid();
        _nonExistentId = Guid.NewGuid();

        // Insert a document but search for a non-existent child
        await _sink.ApplyChanges(new Key(_rootKey, ArrayIndexers.NoIndexers), new Changes.Changeset(
            [
                new Changes.PropertiesChanged<object>(new Dictionary<PropertyPath, object>
                {
                    [new PropertyPath("name")] = "Root Document"
                }),
                new Changes.ChildAdded(
                    new PropertyPath("children"),
                    new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
                    new Dictionary<PropertyPath, object>
                    {
                        [new PropertyPath("childId")] = Guid.NewGuid(),
                        [new PropertyPath("name")] = "Child 1"
                    })
            ]));
    }

    async Task Because() => _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), _nonExistentId);

    [Fact] void should_return_no_value() => _result.HasValue.ShouldBeFalse();
}

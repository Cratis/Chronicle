// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.InMemory.for_InMemorySink.when_trying_to_find_root_key_by_child_value;

public class and_child_is_in_nested_array : given.an_in_memory_sink
{
    Guid _rootKey;
    Guid _configurationId;
    Guid _hubId;
    Monads.Option<Key> _result;

    async Task Establish()
    {
        _rootKey = Guid.NewGuid();
        _configurationId = Guid.NewGuid();
        _hubId = Guid.NewGuid();

        // Insert a document with nested arrays (configurations -> hubs)
        await _sink.ApplyChanges(new Key(_rootKey, ArrayIndexers.NoIndexers), new Changes.Changeset(
            [
                new Changes.PropertiesChanged<object>(new Dictionary<PropertyPath, object>
                {
                    [new PropertyPath("name")] = "Simulation Dashboard"
                }),
                new Changes.ChildAdded(
                    new PropertyPath("configurations"),
                    new Key(_configurationId, ArrayIndexers.NoIndexers),
                    new Dictionary<PropertyPath, object>
                    {
                        [new PropertyPath("configurationId")] = _configurationId,
                        [new PropertyPath("name")] = "Config 1"
                    }),
                new Changes.ChildAdded(
                    new PropertyPath("configurations[0].hubs"),
                    new Key(_hubId, new ArrayIndexers([new ArrayIndexer(new PropertyPath("configurations"), new PropertyPath("configurationId"), _configurationId)])),
                    new Dictionary<PropertyPath, object>
                    {
                        [new PropertyPath("hubId")] = _hubId,
                        [new PropertyPath("name")] = "Hub 1"
                    })
            ]));
    }

    async Task Because() => _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("configurations.hubs.hubId"), _hubId);

    [Fact] void should_return_a_value() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_return_the_root_key() => _result.Value.Value.ShouldEqual(_rootKey);
}

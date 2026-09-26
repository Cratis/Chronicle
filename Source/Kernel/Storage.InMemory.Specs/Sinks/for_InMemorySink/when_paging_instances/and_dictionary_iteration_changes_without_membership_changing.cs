// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_paging_instances;

public class and_dictionary_iteration_changes_without_membership_changing : a_populated_sink<InMemorySinkHarness>
{
    readonly List<string> _names = [];

    async Task Because()
    {
        _names.AddRange(Names(await _sink.GetInstances(take: 2)));
        var collection = ((InMemorySink)_sink).Collection;
        var instance = collection["b"];
        collection.Remove("b");
        collection["b"] = instance;
        _names.AddRange(Names(await _sink.GetInstances(skip: 2, take: 2)));
        _names.AddRange(Names(await _sink.GetInstances(skip: 4, take: 2)));
    }

    [Fact] void should_visit_every_instance_once() => _names.ShouldEqual(["a", "b", "c", "d", "e"]);
}

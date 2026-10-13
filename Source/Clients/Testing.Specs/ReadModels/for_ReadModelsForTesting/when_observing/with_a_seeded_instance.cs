// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_seeded_instance : given.a_collection
{
    async Task Because()
    {
        var id = EventSourceId.New();
        ((ReadModelsForTesting)_store.ReadModels).RegisterInstance(id, new ObservedModule(Guid.Parse(id.Value), "Seeded"));
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_include_the_seeded_instance() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("First", "Seeded");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_seeded_instance_also_produced_by_events : given.a_collection
{
    async Task Because()
    {
        ((ReadModelsForTesting)_store.ReadModels).RegisterInstance(_firstId, new ObservedModule(Guid.Parse(_firstId.Value), "Seeded"));
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_emit_one_instance_per_key() => _emissions.Single().Count().ShouldEqual(1);
    [Fact] void should_replace_the_seeded_instance_with_the_folded_instance() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("First");
}

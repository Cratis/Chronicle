// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_an_inner_surface_without_a_testing_store : given.a_collection
{
    async Task Because()
    {
        var readModels = new ReadModelsForTesting(Substitute.For<IReadModels>(), _store.EventLog);
        _observation = readModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_rebuild_with_default_artifact_discovery() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("First");
}

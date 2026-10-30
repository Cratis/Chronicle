// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_marking_an_active_generation_following<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationTransitionResult _result;
    async Task Because() { _result = await _storage.TryMarkFollowing(_store, _key, MaterializationGeneration.First); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.ShouldEqual(GenerationTransitionResult.Refused);
        await Task.CompletedTask;
    }
}

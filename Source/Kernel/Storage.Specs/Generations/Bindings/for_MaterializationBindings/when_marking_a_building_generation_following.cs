// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_marking_a_building_generation_following<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationTransitionResult _result;
    async Task Establish()
    {
        await Reserve();
    }
    async Task Because() { _result = await _storage.TryMarkFollowing(_store, _key, _candidate); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.ShouldEqual(GenerationTransitionResult.Transitioned);
        await Task.CompletedTask;
    }
}

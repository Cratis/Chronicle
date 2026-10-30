// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_publishing_with_a_stale_epoch<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;
    async Task Establish()
    {
        await Reserve(true);
    }
    async Task Because() { _result = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Unset, _candidate, _rollbackWindow, _receipt); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.Result.ShouldEqual(MaterializationPublicationResult.StaleBinding);
        (await _storage.Get(_store, _key))!.Epoch.ShouldEqual(ServingEpoch.Initial);
        await Task.CompletedTask;
    }
}

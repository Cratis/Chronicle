// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_publish_is_retried_after_a_lost_response<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _original;
    MaterializationPublication _result;
    async Task Establish()
    {
        await Reserve(true);
        _original = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt);
        _time.Advance(TimeSpan.FromMinutes(1));
    }
    async Task Because() { _result = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.ShouldEqual(_original);
        var binding = (await _storage.Get(_store, _key))!;
        binding.Epoch.ShouldEqual(new ServingEpoch(2));
        binding.Generations.Single(_ => _.Identity.Generation == _candidate).PromotedAt.ShouldEqual(_time.Now - TimeSpan.FromMinutes(1));
        binding.Generations.Single(_ => _.Identity.Generation.IsFirst).RollbackDeadline.ShouldEqual(_time.Now - TimeSpan.FromMinutes(1) + _rollbackWindow);
        await Task.CompletedTask;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_publishing_with_the_expected_epoch<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;
    async Task Establish()
    {
        await Reserve(true);
    }
    async Task Because() { _result = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _receipt); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.Result.ShouldEqual(MaterializationPublicationResult.Published);
        _result.Active.ShouldEqual(_candidate);
        _result.Epoch.ShouldEqual(new ServingEpoch(2));
        var binding = await _storage.Get(_store, _key);
        binding!.Destination.Value.ShouldEqual("models-g2");
        binding.Generations.Single(_ => _.Identity.Generation.IsFirst).SupersededBy.ShouldEqual(_candidate);
        binding.Generations.Single(_ => _.Identity.Generation.IsFirst).State.ShouldEqual(MaterializationGenerationState.Retired);
        binding.Fence!.Gate.ShouldEqual(new GenerationGateId(_key, _candidate));
        await Task.CompletedTask;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_restoring_a_retired_generation_within_its_window<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;
    MaterializationBinding _binding;

    async Task Because()
    {
        _result = await Restore();
        _binding = (await _storage.Get(_store, _key))!;
    }

    [Fact] public void should_publish_the_restored_binding()
    {
        _result.ShouldEqual(new MaterializationPublication(MaterializationPublicationResult.Published, MaterializationGeneration.First, _before.Epoch.Next()));
        _binding.Active.ShouldEqual(MaterializationGeneration.First);
        _binding.Destination.ShouldEqual(_first.Identity.Destination);
        _binding.Epoch.ShouldEqual(_before.Epoch.Next());
        _binding.LastReceipt.ShouldEqual(_restoreReceipt);
        _binding.Fence!.Gate.ShouldEqual(new GenerationGateId(_key, MaterializationGeneration.First));
    }

    [Fact] public void should_activate_and_stamp_the_restored_generation()
    {
        var restored = _binding.Generations.Single(_ => _.Identity.Generation.IsFirst);
        restored.State.ShouldEqual(MaterializationGenerationState.Active);
        restored.PromotedAt.ShouldEqual(_time.Now);
        restored.RolledBackFrom.ShouldEqual(_candidate);
        restored.RetiredAt.ShouldBeNull();
        restored.RollbackDeadline.ShouldBeNull();
    }

    [Fact] public void should_retire_the_displaced_generation()
    {
        var retired = _binding.Generations.Single(_ => _.Identity.Generation == _candidate);
        retired.State.ShouldEqual(MaterializationGenerationState.Retired);
        retired.RetiredAt.ShouldEqual(_time.Now);
        retired.RollbackDeadline.ShouldEqual(_time.Now + _rollbackWindow);
        retired.SupersededBy.ShouldEqual(MaterializationGeneration.First);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

[PerFactSpecificationLifecycle] // The refusal fact retries a restore against storage that the other facts inspect.
public abstract class when_restoring_with_a_stale_epoch<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;
    MaterializationPublication _wrongActive;

    async Task Because()
    {
        _result = await _storage.TryRestore(_store, _key, _candidate, ServingEpoch.Initial, MaterializationGeneration.First, _rollbackWindow, _restoreReceipt);
        _wrongActive = await _storage.TryRestore(_store, _key, MaterializationGeneration.First, _before.Epoch, new(99), _rollbackWindow, new(Guid.NewGuid()));
    }

    [Fact] public void should_refuse_the_stale_epoch() => _result.Result.ShouldEqual(MaterializationPublicationResult.StaleBinding);
    [Fact] public void should_check_the_active_before_the_candidate() => _wrongActive.Result.ShouldEqual(MaterializationPublicationResult.StaleBinding);
    [Fact] public Task should_not_change_the_binding() => ShouldKeepBindingUnchanged();
    [Fact] public async Task should_record_the_refusal() => (await Restore()).Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
}

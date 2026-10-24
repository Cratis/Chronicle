// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_restoring_a_generation_that_is_not_retired<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _active;
    MaterializationPublication _missing;

    async Task Because()
    {
        _active = await _storage.TryRestore(_store, _key, _candidate, _before.Epoch, _candidate, _rollbackWindow, _restoreReceipt);
        _missing = await _storage.TryRestore(_store, _key, _candidate, _before.Epoch, new MaterializationGeneration(99), _rollbackWindow, new(Guid.NewGuid()));
    }

    [Fact] public void should_refuse_the_active_generation() => _active.Result.ShouldEqual(MaterializationPublicationResult.CandidateNotRestorable);
    [Fact] public void should_refuse_the_missing_generation() => _missing.Result.ShouldEqual(MaterializationPublicationResult.CandidateNotRestorable);
    [Fact] public Task should_not_change_the_binding() => ShouldKeepBindingUnchanged();
    [Fact] public async Task should_record_the_refusal() => (await Restore()).Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_restore_receipt_is_reused<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _publishAsRestore;
    MaterializationPublication _restoreAsPublish;
    MaterializationPublication _differentWindow;
    MaterializationBinding _restored;

    async Task Because()
    {
        _publishAsRestore = await _storage.TryRestore(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt);
        await Restore();
        _restored = (await _storage.Get(_store, _key))!;
        _restoreAsPublish = await _storage.TryPublish(_store, _key, _candidate, _before.Epoch, MaterializationGeneration.First, _rollbackWindow, _restoreReceipt);
        _differentWindow = await _storage.TryRestore(_store, _key, _candidate, _before.Epoch, MaterializationGeneration.First, _rollbackWindow + TimeSpan.FromHours(1), _restoreReceipt);
    }

    [Fact] public void should_distinguish_publish_from_restore() => _publishAsRestore.Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
    [Fact] public void should_distinguish_restore_from_publish() => _restoreAsPublish.Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
    [Fact] public void should_match_the_rollback_window() => _differentWindow.Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
    [Fact] public async Task should_not_change_the_restored_binding()
    {
        var binding = (await _storage.Get(_store, _key))!;
        binding.Epoch.ShouldEqual(_restored.Epoch);
        binding.Fence.ShouldEqual(_restored.Fence);
        binding.Generations.SequenceEqual(_restored.Generations).ShouldBeTrue();
    }
}

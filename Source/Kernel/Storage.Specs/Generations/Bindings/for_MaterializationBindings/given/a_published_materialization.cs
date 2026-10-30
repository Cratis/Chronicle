// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

public abstract class a_published_materialization<THarness> : a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    protected PublicationReceiptId _restoreReceipt = new(Guid.NewGuid());
    protected MaterializationBinding _before;

    async Task Establish()
    {
        await Reserve(true);
        await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt);
        _before = (await _storage.Get(_store, _key))!;
        _time.Advance(TimeSpan.FromMinutes(10));
    }

    protected Task<MaterializationPublication> Restore() => _storage.TryRestore(_store, _key, _candidate, _before.Epoch, MaterializationGeneration.First, _rollbackWindow, _restoreReceipt);

    protected async Task ShouldKeepBindingUnchanged()
    {
        var binding = (await _storage.Get(_store, _key))!;
        binding.Active.ShouldEqual(_before.Active);
        binding.Epoch.ShouldEqual(_before.Epoch);
        binding.Destination.ShouldEqual(_before.Destination);
        binding.Fence.ShouldEqual(_before.Fence);
        binding.LastReceipt.ShouldEqual(_before.LastReceipt);
        binding.Generations.SequenceEqual(_before.Generations).ShouldBeTrue();
    }
}

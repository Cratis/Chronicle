// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_publication_receipt_is_reused_with_a_different_rollback_window<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;

    async Task Establish()
    {
        await Reserve(true);
        await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt);
    }

    async Task Because() => _result = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow + TimeSpan.FromHours(1), _receipt);

    [Fact] public async Task should_refuse_the_reused_receipt()
    {
        _result.Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
        var retired = (await _storage.Get(_store, _key))!.Generations.Single(_ => _.Identity.Generation.IsFirst);
        retired.RollbackDeadline.ShouldEqual(_time.Now + _rollbackWindow);
    }
}

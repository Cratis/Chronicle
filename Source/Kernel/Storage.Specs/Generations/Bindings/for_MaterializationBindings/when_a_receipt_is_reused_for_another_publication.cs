// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_receipt_is_reused_for_another_publication<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;
    async Task Establish()
    {
        await Reserve(true);
        await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _receipt);
    }
    async Task Because() { _result = await _storage.TryPublish(_store, _key, _candidate, new(2), new(3), _receipt); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.Result.ShouldEqual(MaterializationPublicationResult.ReceiptConflict);
        await Task.CompletedTask;
    }
}

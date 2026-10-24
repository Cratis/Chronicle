// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_publishing_with_the_original_signature<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _result;

    async Task Establish() => await Reserve(true);

    async Task Because() => _result = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _receipt);

    [Fact] public void should_publish() => _result.Result.ShouldEqual(MaterializationPublicationResult.Published);
    [Fact] public async Task should_have_no_rollback_window() => (await _storage.Get(_store, _key))!.Generations.Single(_ => _.Identity.Generation.IsFirst).RollbackDeadline.ShouldEqual(_time.Now);
    [Fact] public async Task should_share_receipt_identity_with_the_explicit_zero_window() => (await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, TimeSpan.Zero, _receipt)).ShouldEqual(_result);
}

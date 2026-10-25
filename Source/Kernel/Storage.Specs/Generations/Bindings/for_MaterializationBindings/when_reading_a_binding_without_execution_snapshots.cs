// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reading_a_binding_without_execution_snapshots<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationExecutionSpecification _execution;
    async Task Because() => _execution = (await ((IGenerationBuildStorage)_storage).GetExecution(_store, _key, MaterializationGeneration.First))!;
    [Fact] public void should_not_invent_execution_provenance() => _execution.ShouldBeNull();
    [Fact] public async Task should_not_invent_build_intent_for_an_adopted_generation() => (await ((IGenerationBuildStorage)_storage).GetBuildRequest(_store, _key, MaterializationGeneration.First)).ShouldBeNull();
    [Fact] public async Task should_not_invent_build_intent_for_an_unknown_generation() => (await ((IGenerationBuildStorage)_storage).GetBuildRequest(_store, _key, new(2))).ShouldBeNull();
    [Fact] public async Task should_still_read_the_old_generation_record() => (await _storage.Get(_store, _key))!.Generations.Single().ShouldEqual(_first);
    [Fact] public async Task should_refuse_legacy_fallback_for_the_cataloged_observer() => (await Catch.Exception(async () => await ((IGenerationBuildStorage)_storage).ResolveObserver(_store, _key.Namespace, EventSequenceId.Log, _first.Identity.ObserverId))).ShouldBeOfExactType<InvalidMaterializationExecution>();
}

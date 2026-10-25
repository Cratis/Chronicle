// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_another_namespace_reserves_the_same_generation_with_another_revision<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _other;
    MaterializationExecutionSpecification _otherExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"another\",\"schema\":{\"type\":\"object\"}}");
    Exception _error;
    GenerationReservation _next;
    async Task Establish()
    {
        await Build();
        _other = _key with { Namespace = "other" };
        await _storage.TryAdoptFirst(_store, _other, _first);
        await _storage.TrySetBindingEpoch(_store, _other, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, _other, _otherExecution);
    }
    async Task Because()
    {
        _error = await Catch.Exception(async () => await _builds.ReserveNext(_store, _other, _otherExecution, _operation, DateTimeOffset.UnixEpoch));
        _next = await _builds.ReserveNext(_store, _other, _otherExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
    }
    [Fact] public void should_refuse_the_generated_observer_collision() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] public async Task should_abandon_the_colliding_generation_in_that_namespace() => (await _storage.Get(_store, _other))!.Generations.Single(_ => _.Identity.Generation == new MaterializationGeneration(2)).State.ShouldEqual(MaterializationGenerationState.Abandoned);
    [Fact] public void should_reserve_the_next_generation_in_that_namespace() => _next.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(3));
    [Fact] public async Task should_keep_the_first_namespace_candidate() => (await _storage.Get(_store, _key))!.Generations.Single(_ => _.Identity.Generation == new MaterializationGeneration(2)).State.ShouldNotEqual(MaterializationGenerationState.Abandoned);
}

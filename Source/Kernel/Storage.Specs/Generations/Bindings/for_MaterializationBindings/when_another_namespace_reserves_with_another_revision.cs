// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_another_namespace_reserves_with_another_revision<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _other;
    MaterializationExecutionSpecification _otherExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"another\",\"schema\":{\"type\":\"object\"}}");
    GenerationReservation _next;
    async Task Establish()
    {
        await Build();
        _other = _key with { Namespace = "other" };
        await _storage.TryAdoptFirst(_store, _other, _first);
        await _storage.TrySetBindingEpoch(_store, _other, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, _other, _otherExecution);
    }
    async Task Because() => _next = await _builds.ReserveNext(_store, _other, _otherExecution, _operation, DateTimeOffset.UnixEpoch);
    [Fact] public void should_reserve_a_new_generation_number() => _next.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(3));
    [Fact] public async Task should_bind_the_number_to_its_revision() => (await _harness.Claims.Get(_store, _next.Record!.Identity.ObserverId))!.Revision.ShouldEqual(_otherExecution.Revision);
    [Fact] public async Task should_not_abandon_anything_in_that_namespace() => (await _storage.Get(_store, _other))!.Generations.Any(_ => _.State == MaterializationGenerationState.Abandoned).ShouldBeFalse();
    [Fact] public async Task should_keep_the_first_namespace_candidate() => (await _storage.Get(_store, _key))!.Generations.Single(_ => _.Identity.Generation == new MaterializationGeneration(2)).State.ShouldNotEqual(MaterializationGenerationState.Abandoned);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_building_in_another_incarnation<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _other;
    GenerationReservation _result;
    async Task Establish()
    {
        await Build();
        _other = _key with { Incarnation = new(Guid.NewGuid()) };
        var first = _first with { Identity = MaterializationGenerationIdentity.For(_other.Projection, MaterializationGeneration.First, "other-models") };
        await _storage.TryAdoptFirst(_store, _other, first);
        await _storage.TrySetBindingEpoch(_store, _other, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, _other, _execution);
    }
    async Task Because() => _result = await _builds.ReserveNext(_store, _other, _execution, _operation, DateTimeOffset.UnixEpoch);
    [Fact] public void should_scope_the_operation_to_its_incarnation() => _result.Result.ShouldEqual(GenerationReservationResult.ReservationCreated);
    [Fact] public async Task should_preserve_both_execution_documents()
    {
        (await _builds.GetExecution(_store, _key, new(2))).ShouldEqual(_execution);
        (await _builds.GetExecution(_store, _other, new(2))).ShouldEqual(_execution);
    }
    [Fact] public async Task should_fail_closed_on_ambiguous_observer_incarnations() => (await Catch.Exception(async () => await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, _result.Record!.Identity.ObserverId))).ShouldBeOfExactType<InvalidMaterializationExecution>();
}

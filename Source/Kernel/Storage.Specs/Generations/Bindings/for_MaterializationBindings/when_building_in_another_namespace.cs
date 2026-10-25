// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_building_in_another_namespace<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _result;
    async Task Establish()
    {
        await Build();
        var other = _key with { Namespace = "other" };
        await _storage.TryAdoptFirst(_store, other, _first);
        await _storage.TrySetBindingEpoch(_store, other, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, other, _execution);
    }
    async Task Because() => _result = await _builds.ReserveNext(_store, _key with { Namespace = "other" }, _execution, _operation, DateTimeOffset.UnixEpoch);
    [Fact] public void should_isolate_operations_and_claims() => _result.Result.ShouldEqual(GenerationReservationResult.ReservationCreated);
    [Fact] public void should_share_the_number_of_the_same_revision() => _result.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(2));
    [Fact] public async Task should_isolate_execution_by_incarnation() => (await _builds.GetExecution(_store, _key with { Incarnation = new(Guid.NewGuid()) }, new(2))).ShouldBeNull();
    [Fact] public async Task should_isolate_execution_by_store() => (await _builds.GetExecution("another-store", _key, new(2))).ShouldBeNull();
    [Fact] public async Task should_not_resolve_a_different_event_sequence() => (await _builds.ResolveObserver(_store, _key.Namespace, "other-sequence", _result.Record!.Identity.ObserverId)).ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reserving_over_a_legacy_observer_definition<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    Exception _error;
    GenerationReservation _next;
    async Task Establish() => await _harness.SaveObserverDefinition(_store, MaterializationGenerationIdentity.For(_key.Projection, new(2), "models").ObserverId);
    async Task Because()
    {
        _error = await Catch.Exception(async () => await Build());
        _next = await Build(new(Guid.NewGuid()));
    }
    [Fact] public void should_refuse_the_generated_observer_collision() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] public async Task should_abandon_the_colliding_generation() => (await _storage.Get(_store, _key))!.Generations.Single(_ => _.Identity.Generation == new MaterializationGeneration(2)).State.ShouldEqual(MaterializationGenerationState.Abandoned);
    [Fact] public void should_reserve_the_next_generation_for_the_next_build() => _next.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(3));
}

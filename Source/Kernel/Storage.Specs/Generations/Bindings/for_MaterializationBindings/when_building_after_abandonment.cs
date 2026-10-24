// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_building_after_abandonment<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _firstReservation;
    GenerationReservation _next;
    async Task Establish()
    {
        _firstReservation = await Build();
        await _storage.TryAbandon(_store, _key, _firstReservation.Record!.Identity.Generation);
    }
    async Task Because() => _next = await Build(new(Guid.NewGuid()));
    [Fact] public void should_never_reuse_the_generation() => _next.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(3));
    [Fact] public async Task should_preserve_the_abandoned_execution() => (await _builds.GetExecution(_store, _key, _firstReservation.Record!.Identity.Generation)).ShouldEqual(_execution);
    [Fact] public async Task should_preserve_the_original_retry() => (await Build()).ShouldEqual(_firstReservation);
}

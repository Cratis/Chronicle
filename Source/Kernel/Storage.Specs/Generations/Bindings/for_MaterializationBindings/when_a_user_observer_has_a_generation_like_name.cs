// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_user_observer_has_a_generation_like_name<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    Exception _error;
    async Task Establish()
    {
        var other = _key with { Projection = "projection@g2" };
        await _storage.TryAdoptFirst(_store, other, new(MaterializationGenerationIdentity.For(other.Projection, MaterializationGeneration.First, "other-models"), MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch));
        await _builds.TryAdoptExecution(_store, other, _execution);
    }
    async Task Because() => _error = await Catch.Exception(async () => await Build());
    [Fact] public void should_refuse_the_generated_observer_collision() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] public async Task should_keep_the_user_projection_identity() => (await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, "projection@g2"))!.Key.Projection.Value.ShouldEqual("projection@g2");
    [Fact] public async Task should_not_allocate_a_partial_candidate() => (await _storage.Get(_store, _key))!.Next.ShouldEqual(MaterializationGeneration.First);
}

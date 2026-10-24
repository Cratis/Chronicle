// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_destination_has_another_owner<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    Exception _error;
    async Task Establish()
    {
        var other = _key with { Projection = "another-projection" };
        await _storage.TryAdoptFirst(_store, other, new(MaterializationGenerationIdentity.For(other.Projection, MaterializationGeneration.First, "models-g2"), MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch));
    }
    async Task Because() => _error = await Catch.Exception(async () => await Build());
    [Fact] public void should_refuse_the_destination_collision() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] public async Task should_not_allocate_a_partial_candidate() => (await _storage.Get(_store, _key))!.Next.ShouldEqual(MaterializationGeneration.First);
}

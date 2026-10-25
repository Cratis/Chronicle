// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_registration_claimed_the_generated_identity<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _result;
    async Task Establish() => await _harness.Claims.Claim(_store, ObserverIdentityClaim.ForRegistration(MaterializationGenerationIdentity.For(_key.Projection, new(2), "models").ObserverId));
    async Task Because() => _result = await Build();
    [Fact] public void should_skip_the_claimed_identity() => _result.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(3));
    [Fact] public async Task should_not_record_the_skipped_number() => (await _storage.Get(_store, _key))!.Generations.Any(_ => _.Identity.Generation == new MaterializationGeneration(2)).ShouldBeFalse();
    [Fact] public async Task should_keep_the_registration_claim() => (await _harness.Claims.Get(_store, MaterializationGenerationIdentity.For(_key.Projection, new(2), "models").ObserverId)).ShouldEqual(ObserverIdentityClaim.ForRegistration(MaterializationGenerationIdentity.For(_key.Projection, new(2), "models").ObserverId));
}

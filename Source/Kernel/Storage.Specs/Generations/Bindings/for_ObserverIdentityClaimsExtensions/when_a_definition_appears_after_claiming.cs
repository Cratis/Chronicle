// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions;

public class when_a_definition_appears_after_claiming : given.a_binding_to_allocate_for
{
    void Establish() => _claims.HasDefinition(_store, IdOf(2)).Returns(Task.FromResult(false), Task.FromResult(true));

    async Task Because() => _result = await _claims.AllocateGeneration(_store, _binding, _revision);

    [Fact] void should_allocate_the_following_number() => _result.ShouldEqual(new MaterializationGeneration(3));
    [Fact] void should_release_the_claim() => _claims.Received(1).Release(_store, Claim(2, _revision));
}

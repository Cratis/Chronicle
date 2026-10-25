// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions;

public class when_allocating_a_free_generation : given.a_binding_to_allocate_for
{
    async Task Because() => _result = await _claims.AllocateGeneration(_store, _binding, _revision);

    [Fact] void should_allocate_the_next_number() => _result.ShouldEqual(new MaterializationGeneration(2));
    [Fact] void should_claim_the_identity_for_the_revision() => _claims.Received(1).Claim(_store, Claim(2, _revision));
}

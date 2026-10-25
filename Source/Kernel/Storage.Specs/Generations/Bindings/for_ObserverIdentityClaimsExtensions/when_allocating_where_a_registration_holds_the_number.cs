// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions;

public class when_allocating_where_a_registration_holds_the_number : given.a_binding_to_allocate_for
{
    void Establish() => Holds(2, ObserverIdentityClaim.ForRegistration(IdOf(2)));

    async Task Because() => _result = await _claims.AllocateGeneration(_store, _binding, _revision);

    [Fact] void should_allocate_the_following_number() => _result.ShouldEqual(new MaterializationGeneration(3));
}

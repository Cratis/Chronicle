// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions;

public class when_allocating_where_the_same_revision_holds_the_number : given.a_binding_to_allocate_for
{
    void Establish() => Holds(2, Claim(2, _revision));

    async Task Because() => _result = await _claims.AllocateGeneration(_store, _binding, _revision);

    [Fact] void should_share_the_number() => _result.ShouldEqual(new MaterializationGeneration(2));
    [Fact] void should_not_claim_again() => _claims.DidNotReceive().Claim(Arg.Any<EventStoreName>(), Arg.Any<ObserverIdentityClaim>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions;

public class when_the_binding_already_used_numbers : given.a_binding_to_allocate_for
{
    void Establish() => _binding = _binding with { Next = new(4) };

    async Task Because() => _result = await _claims.AllocateGeneration(_store, _binding, _revision);

    [Fact] void should_allocate_above_the_bindings_next_number() => _result.ShouldEqual(new MaterializationGeneration(5));
    [Fact] void should_not_probe_a_used_number() => _claims.DidNotReceive().Get(_store, IdOf(4));
}

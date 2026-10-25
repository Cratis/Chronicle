// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings;

public class when_the_binding_owns_the_claim : given.a_later_generation_binding
{
    GenerationObserverBinding? _result;

    void Establish() => _claims.Get("store", _binding.Identity.ObserverId).Returns(ObserverIdentityClaim.ForGeneration(_binding));

    async Task Because() => _result = await Resolve();

    [Fact] void should_resolve_the_binding() => _result.ShouldEqual(_binding);
}

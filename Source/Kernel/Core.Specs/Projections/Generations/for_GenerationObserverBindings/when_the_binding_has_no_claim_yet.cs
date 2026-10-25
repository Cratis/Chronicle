// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings;

public class when_the_binding_has_no_claim_yet : given.a_later_generation_binding
{
    GenerationObserverBinding? _result;

    async Task Because() => _result = await Resolve();

    [Fact] void should_resolve_the_binding() => _result.ShouldEqual(_binding);
}

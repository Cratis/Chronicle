// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings;

public class when_an_observer_is_unbound : Specification
{
    GenerationObserverBinding _result;
    async Task Because() => _result = (await new GenerationObserverBindings(Substitute.For<IGenerationBuildStorage>()).ResolveObserver("store", "ns", EventSequenceId.Log, "user-projection@g2"))!;
    [Fact] void should_not_infer_a_generation_from_the_suffix() => _result.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_the_first_generation_is_bound : given.a_generation_projection_resolver
{
    GenerationObserverBinding _first;
    GenerationExecution? _result;

    void Establish()
    {
        _first = _binding with { Identity = GenerationExecutions.Identity(MaterializationGeneration.First.Value) };
        Bind(_first);
    }

    async Task Because() => _result = await Resolve(_first.Identity.ObserverId);

    [Fact] void should_leave_the_first_generation_to_the_legacy_path() => _result.ShouldBeNull();
    [Fact] void should_not_read_the_execution_snapshot() => _builds.DidNotReceive().GetExecution(Arg.Any<EventStoreName>(), Arg.Any<MaterializationKey>(), Arg.Any<MaterializationGeneration>());
}

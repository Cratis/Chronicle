// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_checking_the_first_generation_observer : given.a_generation_projection_resolver
{
    GenerationObserverBinding _first;
    bool _result;

    void Establish()
    {
        _first = _binding with { Identity = GenerationExecutions.Identity(MaterializationGeneration.First.Value) };
        Bind(_first);
    }

    async Task Because() => _result = await _resolver.IsGenerationObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, _first.Identity.ObserverId);

    [Fact] void should_leave_the_first_generation_to_the_user_projection() => _result.ShouldBeFalse();
}

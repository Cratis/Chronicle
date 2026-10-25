// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_checking_a_generation_observer : given.a_generation_projection_resolver
{
    bool _result;

    void Establish() => Bind(_binding);

    async Task Because() => _result = await _resolver.IsGenerationObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, _binding.Identity.ObserverId);

    [Fact] void should_identify_a_later_generation_observer() => _result.ShouldBeTrue();
}

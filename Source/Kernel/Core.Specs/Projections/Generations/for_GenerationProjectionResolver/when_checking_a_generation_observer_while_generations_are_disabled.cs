// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_checking_a_generation_observer_while_generations_are_disabled : given.a_generation_projection_resolver
{
    bool _result;

    void Establish()
    {
        _enablement.Enabled.Returns(false);
        Bind(_binding);
    }

    async Task Because() => _result = await _resolver.IsGenerationObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, _binding.Identity.ObserverId);

    [Fact] void should_not_identify_a_generation_observer() => _result.ShouldBeFalse();
    [Fact] void should_not_read_the_observer_binding() => _builds.DidNotReceive().ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>());
}

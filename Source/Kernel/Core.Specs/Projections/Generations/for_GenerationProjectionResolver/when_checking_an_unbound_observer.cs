// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_checking_an_unbound_observer : given.a_generation_projection_resolver
{
    bool _result;

    void Establish() => _builds
        .ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
        .Returns((GenerationObserverBinding?)null);

    async Task Because() => _result = await _resolver.IsGenerationObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, GenerationExecutions.Projection);

    [Fact] void should_not_identify_a_generation_observer() => _result.ShouldBeFalse();
}

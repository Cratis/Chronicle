// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_the_projection_id_contains_a_generation_suffix : given.a_generation_projection_resolver
{
    const string UserProjection = "orders@g2";
    GenerationExecution? _result;

    void Establish() => _builds
        .ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
        .Returns((GenerationObserverBinding?)null);

    async Task Because() => _result = await Resolve(UserProjection);

    [Fact] void should_treat_the_observer_as_unbound() => _result.ShouldBeNull();
    [Fact] void should_query_storage_with_the_exact_observer_id() => _builds.Received(1).ResolveObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, new ObserverId(UserProjection));
}

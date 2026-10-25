// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_an_observer_is_unbound : given.a_generation_projection_resolver
{
    GenerationExecution? _result;

    void Establish() => _builds
        .ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
        .Returns((GenerationObserverBinding?)null);

    async Task Because() => _result = await Resolve(GenerationExecutions.Projection);

    [Fact] void should_not_resolve_an_execution() => _result.ShouldBeNull();
    [Fact] void should_not_build_a_projection() => _projectionFactory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_the_current_definition_changes : given.a_generation_bound_subscriber
{
    ProjectionObserverSubscriber _subscriber;

    async Task Establish()
    {
        _subscriber = await Activate();
        _pipelines.ClearReceivedCalls();
        _factory.ClearReceivedCalls();
    }

    Task Because() => _subscriber.OnProjectionDefinitionsChanged(GenerationExecutions.Definition("changedName"));

    [Fact] void should_not_evict_the_generation_pipeline() => _pipelines.DidNotReceive().EvictFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
    [Fact] void should_not_build_a_projection_from_the_changed_definition() => _factory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_not_build_a_legacy_pipeline() => _pipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}

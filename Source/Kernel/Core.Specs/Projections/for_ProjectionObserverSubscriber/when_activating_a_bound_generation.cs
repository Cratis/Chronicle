// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_activating_a_bound_generation : given.a_generation_bound_subscriber
{
    Task Because() => Activate();

    [Fact] void should_admit_the_owner_grant() => _generations.Received(1).Admit(_key.EventStore, _execution);
    [Fact] void should_build_the_pipeline_with_the_admitted_context() => _generationPipelines.Received(1).GetFor(_key.EventStore, _key.Namespace, _generationProjection, _context);
    [Fact] void should_not_read_the_current_definition() => _projectionGrain.DidNotReceive().GetDefinition();
    [Fact] void should_not_subscribe_to_definition_changes() => _projectionGrain.DidNotReceive().SubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>());
    [Fact] void should_not_build_a_projection_from_the_current_definition() => _factory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_never_build_a_legacy_pipeline() => _pipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
    [Fact] void should_not_build_another_generation_pipeline() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Is<GenerationSinkContext>(_ => _ != _context));
}

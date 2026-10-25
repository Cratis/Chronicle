// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_generations_are_disabled : given.a_generation_bound_subscriber
{
    void Establish() => _generations
        .Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
        .Returns((GenerationExecution?)null);

    Task Because() => Activate();

    [Fact] void should_read_the_current_definition() => _projectionGrain.Received(1).GetDefinition();
    [Fact] void should_subscribe_to_definition_changes() => _projectionGrain.Received(1).SubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>());
    [Fact] void should_build_the_projection_from_the_current_definition() => _factory.Received(1).Create(_key.EventStore, _key.Namespace, _currentDefinition, _currentReadModel, Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
    [Fact] void should_build_the_legacy_pipeline() => _pipelines.Received(1).GetFor(_key.EventStore, _key.Namespace, _currentProjection);
    [Fact] void should_not_admit_a_grant() => _generations.DidNotReceive().Admit(Arg.Any<EventStoreName>(), Arg.Any<GenerationExecution>());
    [Fact] void should_not_build_a_generation_pipeline() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>());
}

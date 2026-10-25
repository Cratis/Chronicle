// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_the_execution_snapshot_is_missing : given.a_generation_bound_subscriber
{
    ProjectionObserverSubscriber _subscriber;
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        _generations.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
            .ThrowsAsync(new InvalidMaterializationExecution());
        _subscriber = await Activate();
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [AppendedEvent.EmptyWithEventType(new("event", 1))], new(null));

    [Fact] void should_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_fall_back_to_the_current_definition() => _projectionGrain.DidNotReceive().GetDefinition();
    [Fact] void should_not_build_a_projection() => _factory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_not_build_a_legacy_pipeline() => _pipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}

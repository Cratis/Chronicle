// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_the_owner_grant_is_not_admitted : given.a_generation_bound_subscriber
{
    ProjectionObserverSubscriber _subscriber;
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        _generations.Admit(_key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
        _subscriber = await Activate();
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [AppendedEvent.EmptyWithEventType(new("event", 1))], new(null));

    [Fact] void should_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_build_a_generation_pipeline() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>());
    [Fact] void should_not_fall_back_to_the_current_definition() => _projectionGrain.DidNotReceive().GetDefinition();
    [Fact] void should_not_build_a_projection() => _factory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}

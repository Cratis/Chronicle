// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.given;

public class a_subscriber_with_a_cached_pipeline : Specification
{
    protected ReducerObserverSubscriber _subscriber;
    protected ReducerDefinition _definition;
    protected IReducerPipelineFactory _pipelineFactory;
    protected IReducerMediator _mediator;
    protected IReducerPipeline _firstPipeline;
    protected IReducerPipeline _secondPipeline;
    protected FakeTimeProvider _clock;
    protected readonly ObserverSubscriberKey _key = new("the-reducer", "the-store", "the-namespace", EventSequenceId.Log, "the-partition", string.Empty);

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _definition = CreateDefinition();
        var stateStorage = Substitute.For<IStorage<ReducerDefinition>>();
        stateStorage.State = _definition;
        silo.Options.StorageFactory = _ => stateStorage;

        _firstPipeline = Substitute.For<IReducerPipeline>();
        _secondPipeline = Substitute.For<IReducerPipeline>();
        _pipelineFactory = Substitute.For<IReducerPipelineFactory>();
        var callCount = 0;
        _pipelineFactory.Create(_key.EventStore, _key.Namespace, Arg.Any<ReducerDefinition>()).Returns(_ =>
        {
            callCount++;
            return callCount == 1 ? _firstPipeline : _secondPipeline;
        });
        silo.AddService(_pipelineFactory);

        _mediator = Substitute.For<IReducerMediator>();
        _mediator.When(_ => _.OnNext(
                Arg.Any<ReducerId>(),
                Arg.Any<ConnectionId>(),
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<ReduceOperation>(),
                Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(callInfo => callInfo.Arg<TaskCompletionSource<ReducerSubscriberResult>>().SetResult(
                new ReducerSubscriberResult(ObserverSubscriberResult.Ok(42), null)));
        silo.AddService(_mediator);

        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns(callInfo => callInfo.Arg<ReducerDelegate>()([], null));
        _secondPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns(callInfo => callInfo.Arg<ReducerDelegate>()([], null));

        _clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        silo.AddService<TimeProvider>(_clock);


        _subscriber = await silo.CreateGrainAsync<ReducerObserverSubscriber>(_key.ToString());
        _pipelineFactory.ClearReceivedCalls();
    }

    protected sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        DateTimeOffset _now = now;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    static ReducerDefinition CreateDefinition() => new(
        "the-reducer",
        EventSequenceId.Log,
        [],
        "the-model",
        true,
        []);

    protected static ReadModelDefinition ReadModelDefinition(ReadModelContainerName containerName) => new(
        "the-model",
        containerName,
        "The model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Reducer,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
        []);
}

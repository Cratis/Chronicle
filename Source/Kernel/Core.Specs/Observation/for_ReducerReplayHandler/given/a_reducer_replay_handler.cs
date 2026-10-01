// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.given;

public class a_reducer_replay_handler : Specification
{
    protected ObserverDetails _details;
    protected ReducerReplayHandler _handler;
    protected IReducerMediator _mediator;
    protected IReducerPipeline _pipeline;
    protected ISink _sink;
    protected IReplayContexts _contexts;
    protected IReadModelReplayManager _manager;
    protected IObserver _observer;
    protected ReplayContext _context;
    protected Result<ICanHandleReplayForObserver.Error> _result;

    void Establish()
    {
        _details = new(new("reducer", "event-store", "namespace", EventSequenceId.Log), ObserverType.Reducer);
        var definition = new ReducerDefinition(new("reducer"), EventSequenceId.Log, [], "model", true, []);
        var readModel = new ReadModelDefinition(
            "model",
            "Model",
            "Model",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
            []);
        _context = new(new("model", ReadModelGeneration.First), "Model", "Revert", DateTimeOffset.UtcNow);
        _contexts = Substitute.For<IReplayContexts>();
        _contexts.Establish(Arg.Any<ReadModelType>(), Arg.Any<ReadModelContainerName>()).Returns(_context);
        _contexts.TryGet(readModel.Identifier).Returns(_context);
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        eventStore.Reducers.Get(definition.Identifier).Returns(definition);
        namespaceStorage.ReplayContexts.Returns(_contexts);
        _pipeline = Substitute.For<IReducerPipeline>();
        _pipeline.ReadModel.Returns(readModel);
        _sink = Substitute.For<ISink>();
        _pipeline.Sink.Returns(_sink);
        _pipeline.EndReplay(Arg.Any<ReplayContext>()).Returns(Task.FromResult<IEnumerable<FailedPartition>>([]));
        _pipeline.EndBulk().Returns(Task.FromResult<IEnumerable<FailedPartition>>([]));
        var factory = Substitute.For<IReducerPipelineFactory>();
        factory.Create(_details.Key.EventStore, _details.Key.Namespace, definition).Returns(_pipeline);
        var grainFactory = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IReadModelReplayManager>();
        grainFactory.GetGrain<IReadModelReplayManager>(Arg.Any<string>()).Returns(_manager);
        _observer = Substitute.For<IObserver>();
        grainFactory.GetGrain<IObserver>(_details.Key).Returns(_observer);
        _mediator = Substitute.For<IReducerMediator>();
        _handler = new(_mediator, factory, grainFactory, storage, Options.Create(new Configuration.ChronicleOptions()), NullLogger<ReducerReplayHandler>.Instance);
    }
}

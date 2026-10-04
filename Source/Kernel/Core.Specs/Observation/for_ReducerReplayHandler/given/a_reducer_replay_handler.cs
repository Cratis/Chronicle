// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.given;

public class a_reducer_replay_handler : Specification
{
    protected static readonly ReadModelIdentifier _readModelIdentifier = "TheReadModel";
    protected static readonly ReadModelContainerName _containerName = "theReadModels";
    protected const int ReplayedVersionsToKeep = 3;

    protected ObserverDetails _observerDetails;
    protected IReducerMediator _reducerMediator;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IReducerDefinitionsStorage _reducers;
    protected IReplayContexts _replayContexts;
    protected ISinks _sinks;
    protected ISink _sink;
    protected IReadModel _readModelGrain;
    protected IReadModelReplayManager _replayManager;
    protected IObserver _observer;
    protected ReadModelDefinition _readModel;
    protected ReplayContext _replayContext;
    protected ReducerReplayHandler _handler;

    protected static ICanHandleReplayForObserver.Error? ErrorOf(Result<ICanHandleReplayForObserver.Error> result) =>
        result.TryGetError(out var error) ? error : null;

    void Establish()
    {
        _observerDetails = new(new("TheReducer", "TheEventStore", "TheNamespace", EventSequenceId.Log), ObserverType.Reducer);
        _readModel = new ReadModelDefinition(
            _readModelIdentifier,
            _containerName,
            new ReadModelDisplayName("The read model"),
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _replayContext = new(new(_readModelIdentifier, ReadModelGeneration.First), _containerName, "theReadModels-revert", DateTimeOffset.UtcNow);

        _reducerMediator = Substitute.For<IReducerMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _reducers = Substitute.For<IReducerDefinitionsStorage>();
        _replayContexts = Substitute.For<IReplayContexts>();
        _sinks = Substitute.For<ISinks>();
        _sink = Substitute.For<ISink>();
        _readModelGrain = Substitute.For<IReadModel>();
        _replayManager = Substitute.For<IReadModelReplayManager>();
        _observer = Substitute.For<IObserver>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _eventStoreStorage.Reducers.Returns(_reducers);
        _namespaceStorage.ReplayContexts.Returns(_replayContexts);
        _namespaceStorage.Sinks.Returns(_sinks);
        _sinks.GetFor(_readModel).Returns(_sink);
        _sink.EndReplay(Arg.Any<ReplayContext>()).Returns(Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]));
        _sink.EndBulk().Returns(Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]));

        _reducers.Has(Arg.Any<ReducerId>()).Returns(true);
        _reducers.Get(Arg.Any<ReducerId>()).Returns(new ReducerDefinition("TheReducer", EventSequenceId.Log, [], _readModelIdentifier, true, []));
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModelGrain);
        _readModelGrain.GetDefinition().Returns(_readModel);
        _grainFactory.GetGrain<IReadModelReplayManager>(Arg.Any<string>()).Returns(_replayManager);
        _grainFactory.GetGrain<IObserver>(_observerDetails.Key).Returns(_observer);

        _replayContexts.Establish(Arg.Any<ReadModelType>(), _containerName).Returns(_replayContext);
        _replayContexts.TryGet(_readModelIdentifier).Returns(_replayContext);

        _handler = new(
            _reducerMediator,
            _grainFactory,
            _storage,
            Options.Create(new Configuration.ChronicleOptions { ReadModels = new Configuration.ReadModels { ReplayedVersionsToKeep = ReplayedVersionsToKeep } }),
            NullLogger<ReducerReplayHandler>.Instance);
    }
}

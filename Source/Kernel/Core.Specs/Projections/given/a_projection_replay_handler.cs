// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler : Specification
{
    protected Chronicle.Projections.IProjectionsManager _projections;
    protected IStorage _storage;
    protected IProjectionPipelineManager _projectionPipelineManager;
    protected ProjectionReplayHandler _handler;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    protected IReplayContexts _replayContexts;
    protected IReplayedReadModelsStorage _replayedModels;
    protected IProjectionPipeline _projectionPipeline;
    protected ObserverDetails _observerDetails;
    protected IGrainFactory _grainFactory;
    protected IReadModelReplayManager _readModelReplayManager;

    void Establish()
    {
        _observerDetails = new ObserverDetails(
            new("TheObserver", "TheEventStore", "TheNamespace", EventSequenceId.Log),
            ObserverType.Projection);

        _projections = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _replayContexts = Substitute.For<IReplayContexts>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_eventStoreNamespaceStorage);
        _eventStoreNamespaceStorage.ReplayContexts.Returns(_replayContexts);
        _replayedModels = Substitute.For<IReplayedReadModelsStorage>();
        _eventStoreNamespaceStorage.ReplayedReadModels.Returns(_replayedModels);
        _grainFactory = Substitute.For<IGrainFactory>();
        _readModelReplayManager = Substitute.For<IReadModelReplayManager>();
        _grainFactory.GetGrain<IReadModelReplayManager>(Arg.Any<string>()).Returns(_readModelReplayManager);

        _projectionPipelineManager = Substitute.For<IProjectionPipelineManager>();
        _projectionPipeline = Substitute.For<IProjectionPipeline>();
        _projectionPipelineManager.GetFor(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            Arg.Any<Chronicle.Projections.IProjection>()).Returns(_projectionPipeline);

        _handler = new ProjectionReplayHandler(
            _projections,
            _grainFactory,
            _storage,
            _projectionPipelineManager,
            NullLogger<ProjectionReplayHandler>.Instance);
    }
}

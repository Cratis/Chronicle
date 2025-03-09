// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler : Specification
{
    protected IProjections _projections;
    protected IStorage _storage;
    protected IProjectionPipelineManager _projectionPipelineManager;
    protected ProjectionReplayHandler _handler;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    protected IReplayContexts _replayContexts;
    protected IReplayedModelsStorage _replayedModels;
    protected IProjectionPipeline _projectionPipeline;
    protected ObserverDetails _observerDetails;

    void Establish()
    {
        _observerDetails = new ObserverDetails(
            new ObserverKey("TheObserver", "TheEventStore", "TheNamespace", EventSequenceId.Log),
            ObserverType.Projection);

        _projections = Substitute.For<IProjections>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _replayContexts = Substitute.For<IReplayContexts>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_eventStoreNamespaceStorage);
        _eventStoreNamespaceStorage.ReplayContexts.Returns(_replayContexts);
        _replayedModels = Substitute.For<IReplayedModelsStorage>();
        _eventStoreNamespaceStorage.ReplayedModels.Returns(_replayedModels);

        _projectionPipelineManager = Substitute.For<IProjectionPipelineManager>();
        _projectionPipeline = Substitute.For<IProjectionPipeline>();
        _projectionPipelineManager.GetFor(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            Arg.Any<Chronicle.Projections.IProjection>()).Returns(_projectionPipeline);

        _handler = new ProjectionReplayHandler(_projections, _storage, _projectionPipelineManager);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;

namespace Cratis.Chronicle.Observation.for_PartitionReplayMarkers.given;

public class a_partition_replay_markers_service : Specification
{
    protected IProjectionsManager _projections;
    protected IProjectionPipelineManager _pipelineManager;
    protected IProjectionPipeline _pipeline;
    protected IProjection _projection;
    protected ObserverKey _observerKey;
    protected Key _partition;
    protected PartitionReplayMarkers _markers;

    void Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _pipelineManager = Substitute.For<IProjectionPipelineManager>();
        _pipeline = Substitute.For<IProjectionPipeline>();
        _projection = Substitute.For<IProjection>();
        _observerKey = new("projection-id", "event-store", "namespace", EventSequenceId.Log);
        _partition = (Key)"some-partition";
        _projections.GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>()).Returns(_projection);
        _pipelineManager.GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _projection).Returns(_pipeline);
        _markers = new(_projections, _pipelineManager);
    }
}

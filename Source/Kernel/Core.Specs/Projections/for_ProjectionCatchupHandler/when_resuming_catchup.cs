// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler;

public class when_resuming_catchup : Specification
{
    ProjectionCatchupHandler _handler;
    IProjectionPipeline _pipeline;
    ObserverDetails _details;
    bool _succeeded;

    void Establish()
    {
        _details = new(new("projection", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        var projection = Substitute.For<Engine.IProjection>();
        var projections = Substitute.For<Engine.IProjectionsManager>();
        projections.GetCurrent(_details.Key.EventStore, _details.Key.Namespace, _details.Key.ObserverId).Returns(Task.FromResult<Engine.IProjection?>(projection));
        _pipeline = Substitute.For<IProjectionPipeline>();
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.GetFor(_details.Key.EventStore, _details.Key.Namespace, projection).Returns(Task.FromResult(_pipeline));
        _handler = new(projections, pipelines, Substitute.For<IGrainFactory>(), NullLogger<ProjectionCatchupHandler>.Instance);
    }

    async Task Because() => _succeeded = (await _handler.ResumeCatchupFor(_details)).IsSuccess;

    [Fact] void should_succeed() => _succeeded.ShouldBeTrue();
    [Fact] void should_join_the_open_bulk_window() => _pipeline.Received(1).ResumeBulk();
    [Fact] void should_not_begin_a_new_bulk_window() => _pipeline.DidNotReceive().BeginBulk();
}

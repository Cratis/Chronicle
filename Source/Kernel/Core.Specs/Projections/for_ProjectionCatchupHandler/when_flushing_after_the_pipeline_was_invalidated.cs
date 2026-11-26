// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler;

public class when_flushing_after_the_pipeline_was_invalidated : Specification
{
    readonly BulkWindowId _window = new(Guid.NewGuid());
    ProjectionCatchupHandler _handler;
    ObserverDetails _details;
    IProjectionPipeline _owner;
    BulkFlushResult _result;
    IEnumerable<Storage.Sinks.FailedPartition> _ended;

    void Establish()
    {
        _details = new(new("projection", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        _owner = Substitute.For<IProjectionPipeline>();
        _owner.FlushBulk().Returns(new BulkFlushResult(_window, []));
        _owner.EndBulk(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]));
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.TryGetFor(_details.Key.EventStore, _details.Key.Namespace, _details.Key.ObserverId, out Arg.Any<ProjectionPipelineBinding?>())
            .Returns(call =>
            {
                call[3] = new ProjectionPipelineBinding(Substitute.For<Engine.IProjection>(), _owner);
                return true;
            });
        _handler = new(Substitute.For<Engine.IProjectionsManager>(), pipelines, Substitute.For<IGrainFactory>(), NullLogger<ProjectionCatchupHandler>.Instance);
    }

    async Task Because()
    {
        _result = await _handler.FlushFor(_details);
        _ended = await _handler.EndBulkFor(_details, CancellationToken.None);
    }

    [Fact] void should_flush_the_window_of_the_session_owner() => _result.WindowId.ShouldEqual(_window);
    [Fact] void should_end_the_bulk_session_on_the_session_owner() => _owner.Received(1).EndBulk(Arg.Any<CancellationToken>());
    [Fact] void should_report_no_failed_partitions() => _ended.ShouldBeEmpty();
}

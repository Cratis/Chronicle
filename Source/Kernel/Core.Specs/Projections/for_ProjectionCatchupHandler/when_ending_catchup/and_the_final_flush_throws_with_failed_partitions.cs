// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_ending_catchup;

public class and_the_final_flush_throws_with_failed_partitions : Specification
{
    readonly Key _partition = "partition";
    ObserverDetails _details;
    IObserver _observer;
    ProjectionCatchupHandler _handler;

    void Establish()
    {
        _details = new(new("observer", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        var projection = Substitute.For<Engine.IProjection>();
        var projections = Substitute.For<Engine.IProjectionsManager>();
        projections.TryGet(_details.Key.EventStore, _details.Key.Namespace, _details.Key.ObserverId, out _)
            .Returns(info =>
            {
                info[3] = projection;
                return true;
            });
        var pipeline = Substitute.For<IProjectionPipeline>();
        pipeline.EndBulk().Returns(Task.FromException<IEnumerable<SinkFailedPartition>>(new BulkWriteFailed([new(_partition, 42UL)], new BulkServerUnavailable())));
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.GetFor(_details.Key.EventStore, _details.Key.Namespace, projection).Returns(pipeline);
        var grains = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        grains.GetGrain<IObserver>(_details.Key).Returns(_observer);
        _handler = new(projections, pipelines, grains, NullLogger<ProjectionCatchupHandler>.Instance);
    }

    async Task Because() => await _handler.EndCatchupFor(_details);

    [Fact] void should_record_the_failures_carried_by_the_exception() => _observer.Received(1).PartitionFailed(_partition, 42UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);

    sealed class BulkServerUnavailable() : Exception("The server did not answer");
}

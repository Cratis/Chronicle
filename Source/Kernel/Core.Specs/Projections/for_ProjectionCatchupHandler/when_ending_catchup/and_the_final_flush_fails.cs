// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_ending_catchup;

public class and_the_final_flush_fails : Specification
{
    readonly Key _partition = "partition";
    readonly EventSequenceNumber _sequenceNumber = 42UL;
    ObserverDetails _observerDetails;
    IObserver _observer;
    IProjectionPipeline _pipeline;
    Result<ICanHandleCatchupForObserver.Error> _result;
    ProjectionCatchupHandler _handler;

    void Establish()
    {
        _observerDetails = new(new("observer", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        var projection = Substitute.For<Engine.IProjection>();
        var projections = Substitute.For<Engine.IProjectionsManager>();
        projections.TryGet(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.ObserverId, out _)
            .Returns(info =>
            {
                info[3] = projection;
                return true;
            });
        _pipeline = Substitute.For<IProjectionPipeline>();
        _pipeline.EndBulk().Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([new(_partition, _sequenceNumber)]));
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, projection).Returns(_pipeline);
        var grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        grainFactory.GetGrain<IObserver>(_observerDetails.Key).Returns(_observer);
        _handler = new(projections, pipelines, grainFactory, NullLogger<ProjectionCatchupHandler>.Instance);
    }

    async Task Because() => _result = await _handler.EndCatchupFor(_observerDetails);

    [Fact] void should_record_the_failed_partition() => _observer.Received(1).PartitionFailed(_partition, _sequenceNumber, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_report_a_finalization_error() => _result.TryGetError(out _).ShouldBeTrue();
}

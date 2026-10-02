// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Monads;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_flushing_replay;

public class and_the_flush_leaves_failed_partitions : given.a_projection_replay_handler_with_projection
{
    readonly Key _partition = "partition";
    readonly EventSequenceNumber _sequenceNumber = 42UL;
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _projectionPipeline.EndBulk().Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([new(_partition, _sequenceNumber)]));

    async Task Because() => _result = await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_record_the_failed_partition() => _observer.Received(1).PartitionFailed(_partition, _sequenceNumber, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_report_an_error() => _result.TryGetError(out _).ShouldBeTrue();
}

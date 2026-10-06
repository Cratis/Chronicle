// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sinks;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_flushing_replay;

public class and_the_flush_throws_with_failed_partitions : given.a_projection_replay_handler_with_projection
{
    readonly Key _partition = "partition";

    void Establish() => _projectionPipeline.EndBulk().Returns(Task.FromException<IEnumerable<SinkFailedPartition>>(new BulkWriteFailed([new(_partition, 42UL)], new BulkServerUnavailable())));

    async Task Because() => await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_record_the_failures_carried_by_the_exception() => _observer.Received(1).PartitionFailed(_partition, 42UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);

    sealed class BulkServerUnavailable() : Exception("The server did not answer");
}

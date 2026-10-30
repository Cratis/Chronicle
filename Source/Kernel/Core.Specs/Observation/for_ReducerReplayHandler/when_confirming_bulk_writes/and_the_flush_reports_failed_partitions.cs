// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_confirming_bulk_writes;

public class and_the_flush_reports_failed_partitions : given.a_reducer_replay_handler
{
    readonly Storage.Sinks.FailedPartition _failure = new("some-partition", 42UL);
    BulkFlushResult _result;

    void Establish() => _sink.FlushBulk().Returns(new BulkFlushResult(new(Guid.NewGuid()), [_failure]));

    async Task Because() => _result = await ((ICanHandleReplayForObserver)_handler).FlushFor(_observerDetails);

    [Fact] void should_return_the_failure() => _result.FailedPartitions.ShouldContain(_failure);
    [Fact] void should_record_the_failed_partition() => _observer.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed) && Equals(call.GetArguments()[4], FailureKind.Handling)).ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_flushing_replay;

public class and_the_flush_throws_with_failed_partitions : given.a_reducer_replay_handler
{
    readonly FailedPartition[] _failures = [new("first", 1UL), new("second", 2UL)];
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _sink.EndBulk().Returns(Task.FromException<IEnumerable<FailedPartition>>(new BulkWriteFailed(_failures, new FlushInterrupted())));

    async Task Because() => _result = await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_report_failure() => ErrorOf(_result).ShouldEqual(ICanHandleReplayForObserver.Error.Unknown);
    [Fact] void should_record_all_partitions_once() => _observer.Received(1).PartitionsFailed(Arg.Is<IReadOnlyCollection<FailedPartition>>(partitions => partitions.SequenceEqual(_failures)));

    sealed class FlushInterrupted() : Exception("The flush was interrupted");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_the_final_flush_leaves_failed_partitions : given.a_reducer_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _sink.EndReplay(_replayContext).Returns(Task.FromResult<IEnumerable<FailedPartition>>([new FailedPartition("partition", EventSequenceNumber.First)]));

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_report_a_failure() => ErrorOf(_result).ShouldEqual(ICanHandleReplayForObserver.Error.Unknown);
    [Fact] void should_record_the_failed_partition() => _observer.Received(1).PartitionFailed(Arg.Any<Key>(), EventSequenceNumber.First, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<Concepts.Observation.FailureKind>());
    [Fact] void should_not_mark_the_read_model_as_replayed() => _replayManager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
}

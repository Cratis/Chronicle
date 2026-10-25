// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_nothing;

public class and_the_replay_only_partially_completed : given.an_observer_with_replaying_partition
{
    void Establish() => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(EventSequenceNumber.First, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns((EventSequenceNumber)50UL);

    async Task Because() => await _observer.PartitionReplayPartiallyCompleted(_partition, EventSequenceNumber.Unavailable);

    [Fact] void should_stop_holding_back_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_only_excluded_events;

/// <summary>
/// The replay ended with no read position at all, so there is no position to catch up from. Events held back while
/// it replayed are handed to catch-up from the start of the partition.
/// </summary>
public class and_the_replay_read_nothing_and_an_event_arrived_while_it_replayed : given.an_observer_with_replaying_partition
{
    void Establish() => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(EventSequenceNumber.First, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns((EventSequenceNumber)50UL);

    async Task Because() => await _observer.PartitionReplayed(_partition, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, []);

    [Fact] void should_stop_holding_back_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_start_catchup_job_from_the_start_of_the_partition() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition && _.FromSequenceNumber == EventSequenceNumber.First));
}

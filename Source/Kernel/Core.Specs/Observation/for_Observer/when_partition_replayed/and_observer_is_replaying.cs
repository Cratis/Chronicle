// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_observer_is_replaying : given.an_observer_with_replaying_partition
{
    EventSequenceNumber _newSequenceNumber;

    async Task Establish()
    {
        _newSequenceNumber = _lastHandledEventSequenceNumber.Next();

        // There is an event left for the partition, so replaying is the only thing standing between the
        // partition being replayed and a catch-up job being started.
        EventSequenceHasNextEvent(_newSequenceNumber);
        await ObserverIsReplaying();

        // Routing clears the replaying partitions on its way to the replaying state, so the partition this
        // spec is about has to be put back before the observer is told it was replayed.
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _storageStats.ResetCounts();
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, _newSequenceNumber);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_remove_partition_from_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_update_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_newSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_partition_replayed.not_needing_catchup;

public class and_last_handled_event_is_greater_than_current_last_handled_and_next_event_sequence_number : given.all_dependencies
{
    static EventSequenceNumber _newSequenceNumber;

    void Establish()
    {
        _newSequenceNumber = _nextEventSequenceNumber.Next();
        EventSequenceDoesNotHaveNextEvent(_newSequenceNumber);
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, _newSequenceNumber);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_remove_partition_from_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_update_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_newSequenceNumber);
    [Fact] void should_update_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_newSequenceNumber.Next());
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up.not_needing_catchup;

public class and_last_handled_event_is_greater_than_current_last_handled : given.an_observer_with_one_partition_being_caught_up
{
    static EventSequenceNumber _newSequenceNumber;

    void Establish()
    {
        _newSequenceNumber = _lastHandledEventSequenceNumber.Next();
        EventSequenceDoesNotHaveNextEvent(_newSequenceNumber);
    }

    async Task Because() => await _observer.PartitionCaughtUp(_partition, _newSequenceNumber);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_remove_partition_from_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_update_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_newSequenceNumber);
    [Fact] void should_not_update_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up.needing_catchup;

public class and_last_handled_event_is_less_than_current_last_handled : given.an_observer_with_one_partition_being_caught_up
{
    static EventSequenceNumber _newSequenceNumber;

    void Establish()
    {
        _newSequenceNumber = _lastHandledEventSequenceNumber - 1;
        EventSequenceHasNextEvent(_newSequenceNumber);
    }

    async Task Because() => await _observer.PartitionCaughtUp(_partition, _newSequenceNumber);

    [Fact] void should_write_state_twice() => _storageStats.Writes.ShouldEqual(2);
    [Fact] void should_still_contain_partition_in_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_not_update_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_not_update_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_start_catchup_job() => CheckStartedCatchupJob(_newSequenceNumber);
}

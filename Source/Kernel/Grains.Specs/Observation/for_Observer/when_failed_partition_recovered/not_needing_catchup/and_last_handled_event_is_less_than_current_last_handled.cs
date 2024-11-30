// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_failed_partition_recovered.not_needing_catchup;

public class and_last_handled_event_is_less_than_current_last_handled : given.all_dependencies
{
    static EventSequenceNumber _newSequenceNumber;

    void Establish()
    {
        _newSequenceNumber = _lastHandledEventSequenceNumber - 1;
        EventSequenceDoesNotHaveNextEvent(_newSequenceNumber);
    }

    async Task Because() => await _observer.FailedPartitionRecovered(_partition, _newSequenceNumber);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_remove_partition_from_failed_partitions() => _failedPartitionsStorage.State.Partitions.Select(_ => _.Partition).ShouldNotContain(_partition);
    [Fact] void should_not_update_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_not_update_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

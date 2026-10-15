// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered.having_read_only_excluded_events;

/// <summary>
/// The recovery read only events the observer's filters exclude, and an event for the partition was appended after
/// what it read but before the failure was cleared. Live delivery skipped that event because the partition was
/// failed, so clearing the failure must hand it to catch-up - from what the recovery read, not from what it handled.
/// </summary>
public class and_an_event_arrived_after_what_was_read : given.all_dependencies
{
    static EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceHasNextEvent(_read);
    }

    async Task Because() => await _observer.FailedPartitionRecovered(_partition, EventSequenceNumber.Unavailable, _read);

    [Fact] void should_remove_partition_from_failed_partitions() => _failedPartitionsStorage.State.Partitions.Select(_ => _.Partition).ShouldNotContain(_partition);
    [Fact] void should_decrement_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_not_count_any_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_start_catchup_job_after_what_was_read() => CheckStartedCatchupJob(_read);
}

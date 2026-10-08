// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered.having_read_only_excluded_events;

/// <summary>
/// The recovery read only events the observer's filters exclude and nothing arrived after them. The failure is
/// cleared without counting any event as handled, and the excluded events do not start a catch-up that reads them again.
/// </summary>
public class and_nothing_arrived_after_what_was_read : given.all_dependencies
{
    static EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceDoesNotHaveNextEvent(_read);
    }

    async Task Because() => await _observer.FailedPartitionRecovered(_partition, EventSequenceNumber.Unavailable, _read);

    [Fact] void should_remove_partition_from_failed_partitions() => _failedPartitionsStorage.State.Partitions.Select(_ => _.Partition).ShouldNotContain(_partition);
    [Fact] void should_not_count_any_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

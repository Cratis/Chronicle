// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_only_excluded_events;

/// <summary>
/// The replay read only events the observer's filters exclude, so it handled nothing. Live delivery held back the
/// partition while it replayed, so the replay marker must still come down, and an event that arrived meanwhile is
/// handed to catch-up from what the replay read.
/// </summary>
public class and_an_event_arrived_while_it_replayed : given.an_observer_with_replaying_partition
{
    static EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceHasNextEvent(_read);
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, EventSequenceNumber.Unavailable, _read, []);

    [Fact] void should_stop_holding_back_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_count_any_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_start_catchup_job_after_what_was_read() => CheckStartedCatchupJob(_read);
}

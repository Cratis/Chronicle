// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_only_excluded_events;

/// <summary>
/// The replay read only events the observer's filters exclude and nothing arrived meanwhile. The replay marker comes
/// down, and the excluded events do not start a catch-up that reads them again.
/// </summary>
public class and_nothing_arrived_while_it_replayed : given.an_observer_with_replaying_partition
{
    static EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceDoesNotHaveNextEvent(_read);
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, EventSequenceNumber.Unavailable, _read, []);

    [Fact] void should_stop_holding_back_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

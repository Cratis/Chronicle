// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_nothing;

/// <summary>
/// The replay stopped, failed before its first event, or covered only selected event types and read nothing.
/// The job reports all of these with no replayed event types, so there is no proof every event of the partition
/// was read and a catch-up from the start would redeliver events that were already handled.
/// </summary>
public class and_the_replay_did_not_cover_every_event_type : given.an_observer_with_replaying_partition
{
    void Establish() => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(EventSequenceNumber.First, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns((EventSequenceNumber)50UL);

    async Task Because() => await _observer.PartitionReplayed(_partition, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, []);

    [Fact] void should_stop_holding_back_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_count_any_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}

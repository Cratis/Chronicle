// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// The step read only excluded events, and another event for the partition arrived after them. Live delivery skips a
/// failed partition, so that event reaches the observer only through catch-up. Looking for it here would race with
/// appends made before the observer clears the failure, so the job hands the observer what it read instead - the
/// observer clears the failure and looks for later events in the same turn.
/// </summary>
public class and_an_event_arrived_after_the_excluded_events_were_read : given.a_retry_failed_partition_job
{
    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.LastScannedEventSequenceNumber = 7UL;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .Returns((EventSequenceNumber)8UL);
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_hand_what_was_read_to_the_observer() => _observer.Received(1).FailedPartitionRecovered(_request.Key, EventSequenceNumber.Unavailable, (EventSequenceNumber)7UL);
    [Fact] void should_leave_looking_for_later_events_to_the_observer() => _eventSequenceStorage.DidNotReceive().GetNextSequenceNumberGreaterOrEqualThan(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>());
    [Fact] void should_not_keep_the_partition_failed() => _observer.DidNotReceive().FailedPartitionNotRecovered(Arg.Any<Key>());
}

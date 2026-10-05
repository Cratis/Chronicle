// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// The step read only excluded events, but another event for the partition arrived after them. Live delivery skips
/// a failed partition, so clearing the failure now would lose that event - keep it failed for the next retry.
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

    [Fact] void should_not_resolve_the_failed_partition() => _observer.DidNotReceive().FailedPartitionRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

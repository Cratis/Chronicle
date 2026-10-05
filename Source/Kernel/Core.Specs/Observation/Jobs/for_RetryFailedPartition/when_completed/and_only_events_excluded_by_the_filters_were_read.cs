// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// Every event left in the failed partition is excluded by the observer's filters, so there is nothing for the
/// subscriber to handle. The failure is resolved without any event being reported as handled.
/// </summary>
public class and_only_events_excluded_by_the_filters_were_read : given.a_retry_failed_partition_job
{
    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.LastScannedEventSequenceNumber = 7UL;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                _request.FromSequenceNumber,
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .Returns(_request.FromSequenceNumber);
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_resolve_the_failed_partition_without_claiming_a_handled_event() => _observer.Received(1).FailedPartitionRecovered(_request.Key, EventSequenceNumber.Unavailable);
    [Fact] void should_look_for_events_only_after_what_was_read() => _eventSequenceStorage.Received(1).GetNextSequenceNumberGreaterOrEqualThan((EventSequenceNumber)8UL, Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>());
}

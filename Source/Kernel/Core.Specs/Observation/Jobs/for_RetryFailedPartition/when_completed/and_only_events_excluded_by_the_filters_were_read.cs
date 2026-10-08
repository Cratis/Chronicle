// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// Every event left in the failed partition is excluded by the observer's filters, so there is nothing for the
/// subscriber to handle. The failure is resolved without any event being reported as handled, and the observer is
/// told how far the recovery read so it can catch up anything that arrived after it.
/// </summary>
public class and_only_events_excluded_by_the_filters_were_read : given.a_retry_failed_partition_job
{
    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.LastScannedEventSequenceNumber = 7UL;
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_resolve_the_failed_partition_from_what_was_read_without_claiming_a_handled_event() => _observer.Received(1).FailedPartitionRecovered(_request.Key, EventSequenceNumber.Unavailable, (EventSequenceNumber)7UL);
}

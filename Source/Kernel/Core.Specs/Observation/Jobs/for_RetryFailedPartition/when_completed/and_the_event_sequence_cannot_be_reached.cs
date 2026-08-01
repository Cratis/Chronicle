// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// Without an answer from the event sequence there is no evidence the failure record is stale. Keeping the
/// partition failed costs another retry; clearing it on a guess loses the work permanently.
/// </summary>
public class and_the_event_sequence_cannot_be_reached : given.a_retry_failed_partition_job
{
    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .ThrowsAsync(new TimeoutException());
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_not_call_failed_partition_recovered() => _observer.DidNotReceive().FailedPartitionRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

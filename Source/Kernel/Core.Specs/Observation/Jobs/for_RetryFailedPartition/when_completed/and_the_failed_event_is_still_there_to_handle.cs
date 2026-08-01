// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// A step that succeeded having read nothing while the event it failed on is still in the sequence is not a
/// stale failure record. Clearing it advances the observer past that event without the handler ever running -
/// the missed side effect is lost for good and the observer reports healthy, so nothing ever prompts a look.
/// Keep the partition failed and let the next retry try again.
/// </summary>
public class and_the_failed_event_is_still_there_to_handle : given.a_retry_failed_partition_job
{
    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .Returns(_request.FromSequenceNumber);
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_not_call_failed_partition_recovered() => _observer.DidNotReceive().FailedPartitionRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_call_failed_partition_partially_recovered() => _observer.DidNotReceive().FailedPartitionPartiallyRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

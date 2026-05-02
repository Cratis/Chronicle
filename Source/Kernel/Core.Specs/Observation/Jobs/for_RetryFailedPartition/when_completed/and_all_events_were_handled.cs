// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

public class and_all_events_were_handled : given.a_retry_failed_partition_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_call_failed_partition_recovered() => _observer.Received(1).FailedPartitionRecovered((Key)"some-partition", _lastHandled);
    [Fact] void should_not_call_failed_partition_partially_recovered() => _observer.DidNotReceive().FailedPartitionPartiallyRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

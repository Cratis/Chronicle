// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// When the step succeeded but found no events (HandledAllEvents=true, no actual sequence number),
/// the observer already advanced its NextEventSequenceNumber past the failed event — the events were
/// processed despite the caller timing out. The stale failed-partition record must be cleared.
/// </summary>
public class and_observer_already_caught_up : given.a_retry_failed_partition_job
{
    void Establish() => _stateStorage.State.HandledAllEvents = true;

    async Task Because() => await _job.Start(_request);

    [Fact] void should_call_failed_partition_recovered() => _observer.Received(1).FailedPartitionRecovered(_request.Key, _request.FromSequenceNumber);
    [Fact] void should_not_call_failed_partition_partially_recovered() => _observer.DidNotReceive().FailedPartitionPartiallyRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

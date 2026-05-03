// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// When the step ran but the subscriber failed every event — HandledAllEvents stays false.
/// The partition is still legitimately failed; do not clear it.
/// </summary>
public class and_no_events_were_handled : given.a_retry_failed_partition_job
{
    async Task Because() => await _job.Start(_request);

    [Fact] void should_not_call_failed_partition_recovered() => _observer.DidNotReceive().FailedPartitionRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_call_failed_partition_partially_recovered() => _observer.DidNotReceive().FailedPartitionPartiallyRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}

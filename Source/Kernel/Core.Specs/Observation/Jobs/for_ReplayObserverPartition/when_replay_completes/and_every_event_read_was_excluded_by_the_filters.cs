// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

/// <summary>
/// The partition holds events, but the observer's filters exclude every one of them, so the replay handles nothing.
/// The observer still has to hear that the replay is over - otherwise the partition stays marked as replaying and
/// live delivery holds back its matching events for good - and learn how far the replay read.
/// </summary>
public class and_every_event_read_was_excluded_by_the_filters : given.a_partition_replay_job
{
    static readonly EventSequenceNumber _lastScanned = 42UL;

    void Establish()
    {
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.LastScannedEventSequenceNumber = _lastScanned;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_complete_the_partition_replay_without_claiming_a_handled_event() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, _lastScanned, Arg.Any<EventType[]>());
}

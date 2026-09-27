// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_replay_was_partial : given.an_observer_with_replaying_partition
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        EventSequenceDoesNotHaveNextEvent(_lastHandledEventSequenceNumber);
    }

    async Task Because() => await _observer.PartitionReplayPartiallyCompleted(_partition, _lastHandledEventSequenceNumber);

    [Fact] void should_keep_failed_partition() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_keep_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_stop_marking_partition_as_replaying() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_persist_failures() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
}

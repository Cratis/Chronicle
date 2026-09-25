// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_failed_partition_was_replayed : given.an_observer_with_replaying_partition
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        EventSequenceDoesNotHaveNextEvent(_lastHandledEventSequenceNumber);
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, _lastHandledEventSequenceNumber);

    [Fact] void should_resolve_failed_partition() => _failedPartitionsStorage.State.ResolvedPartitions.Single().IsResolved.ShouldBeTrue();
    [Fact] void should_remove_failed_partition() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_decrement_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_persist_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_persist_observer_state() => _storageStats.Writes.ShouldEqual(1);
}

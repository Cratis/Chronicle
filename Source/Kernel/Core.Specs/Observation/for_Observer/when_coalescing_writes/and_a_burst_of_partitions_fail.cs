// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_a_burst_of_partitions_fail : given.an_observer_coalescing_writes
{
    const int Failures = 25;

    int _stateWritesBeforeFlush;
    int _wholeDocumentWritesBeforeFlush;

    async Task Because()
    {
        for (var i = 0; i < Failures; i++)
        {
            await _observer.PartitionFailed($"partition-{i}", 42UL, ["Something went wrong"], "stack");
        }

        _stateWritesBeforeFlush = _storageStats.Writes;
        _wholeDocumentWritesBeforeFlush = _failedPartitionsStorageStats.Writes;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_write_the_observer_state_once_per_failure() => _stateWritesBeforeFlush.ShouldEqual(0);
    [Fact] void should_not_write_the_whole_failed_partitions_document_per_failure() => _wholeDocumentWritesBeforeFlush.ShouldEqual(0);
    [Fact] void should_write_each_failed_partition_on_its_own() => _eventStoreNamespaceStorage.FailedPartitions.Received(Failures).Save(_observerId, Arg.Is<FailedPartitions>(_ => _.Partitions.Count() == 1));
    [Fact] void should_write_the_observer_state_once_when_flushed() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_count_every_failure() => _stateStorage.State.FailedPartitionCount.Value.ShouldEqual(Failures);
}

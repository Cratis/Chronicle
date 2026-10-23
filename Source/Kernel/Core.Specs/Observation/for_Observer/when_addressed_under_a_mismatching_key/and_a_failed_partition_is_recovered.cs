// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

public class and_a_failed_partition_is_recovered : given.an_observer_under_a_mismatching_key
{
    static readonly Key _partition = "a-partition";

    async Task Establish()
    {
        _failedPartitionsStorage.State = new FailedPartitions { Partitions = [new() { Partition = _partition, ObserverId = _observerId }] };
        await Crash();
        _failedPartitionsStorageStats.ResetCounts();
    }

    async Task Because() => await _observer.FailedPartitionRecovered(_partition, 42UL);

    [Fact] void should_not_write_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_save_failed_partitions_to_storage() => await _eventStoreNamespaceStorage.FailedPartitions.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
    [Fact] void should_not_write_state() => _persistedStateStorage!.Stats.Writes.ShouldEqual(0);
}

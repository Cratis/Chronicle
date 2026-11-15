// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_operator_retry_of_an_exhausted_partition_succeeds : given.an_exhausted_partition
{
    async Task Because()
    {
        await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);
        await _observer.FailedPartitionRecovered((Key)Partition, LastFailedSequenceNumber);
    }

    [Fact] void should_remove_the_failure() => _failedPartitionsState.Partitions.ShouldBeEmpty();
    [Fact] void should_persist_the_removal() => _failedPartitionsStorageStats.Writes.ShouldEqual(2);
}

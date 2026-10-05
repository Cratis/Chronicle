// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_not_recovered;

/// <summary>
/// The retry reminder is removed when a recovery starts. A recovery that ends without recovering the partition and
/// without recording a new failure must leave another retry scheduled, or the partition stays failed for good.
/// </summary>
public class and_the_partition_is_still_failed : given.an_observer
{
    const string Partition = "Something";

    void Establish() => _failedPartitionsState.AddFailedPartition(Partition, 42UL);

    async Task Because() => await _observer.FailedPartitionNotRecovered(Partition);

    [Fact] void should_schedule_another_retry() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_keep_the_partition_failed() => _failedPartitionsState.Partitions.Select(_ => _.Partition.ToString()).ShouldContain(Partition);
}

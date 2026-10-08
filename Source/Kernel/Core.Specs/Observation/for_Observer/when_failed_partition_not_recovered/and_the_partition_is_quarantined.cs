// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_not_recovered;

/// <summary>
/// A quarantined partition is retried only once an operator clears the quarantine, so a recovery that ended without
/// recovering it schedules nothing.
/// </summary>
public class and_the_partition_is_quarantined : given.an_observer
{
    const string Partition = "Something";

    void Establish()
    {
        _failedPartitionsState.AddFailedPartition(Partition, 42UL);
        _failedPartitionsState.Quarantine(Partition);
    }

    async Task Because() => await _observer.FailedPartitionNotRecovered(Partition);

    [Fact] void should_not_schedule_a_retry() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_partition_quarantine;

public class and_not_retrying : given.a_quarantined_partition
{
    ClearPartitionQuarantineResult _result;

    async Task Because() => _result = await _observer.ClearPartitionQuarantine((Key)Partition, false);

    [Fact] void should_report_cleared() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Cleared);
    [Fact] void should_not_be_quarantined() => FailedPartition.IsQuarantined.ShouldBeFalse();
    [Fact] void should_keep_all_attempts() => FailedPartition.Attempts.Count().ShouldEqual(3);
    [Fact] void should_reset_the_retry_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_write_the_failed_partitions_to_storage() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_change_the_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_register_a_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(FailedPartition.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}

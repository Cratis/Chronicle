// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_partition_quarantine;

public class and_the_observer_is_quarantined : given.a_quarantined_partition
{
    ClearPartitionQuarantineResult _result;

    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _failedPartitionsStorageStats.ResetCounts();
    }

    async Task Because() => _result = await _observer.ClearPartitionQuarantine((Key)Partition, true);

    [Fact] void should_still_clear_the_partition() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Cleared);
    [Fact] void should_not_be_quarantined() => FailedPartition.IsQuarantined.ShouldBeFalse();
    [Fact] void should_report_that_the_observer_is_quarantined() => _result.RetryOutcome.ShouldEqual(PartitionRecoveryOutcome.ObserverQuarantined);
    [Fact] void should_write_the_failed_partitions_to_storage() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}

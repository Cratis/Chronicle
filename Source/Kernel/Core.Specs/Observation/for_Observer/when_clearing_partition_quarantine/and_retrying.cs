// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_partition_quarantine;

public class and_retrying : given.a_quarantined_partition
{
    ClearPartitionQuarantineResult _result;

    async Task Because() => _result = await _observer.ClearPartitionQuarantine((Key)Partition, true);

    [Fact] void should_report_cleared() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Cleared);
    [Fact] void should_report_the_retry_started() => _result.RetryOutcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_write_the_failed_partitions_to_storage() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_start_a_recover_job_from_the_last_attempt() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.Key == (Key)Partition &&
                _.FromSequenceNumber == LastFailedSequenceNumber));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_operator_retries_an_exhausted_partition : given.an_exhausted_partition
{
    PartitionRecoveryOutcome _outcome;

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_start_recovery() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_start_a_job_from_the_failed_position() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ =>
            _.Key == (Key)Partition && _.FromSequenceNumber == LastFailedSequenceNumber));
    [Fact] void should_release_the_exhaustion_quarantine() => FailedPartition.IsQuarantined.ShouldBeFalse();
    [Fact] void should_preserve_the_attempt_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(3);
    [Fact] void should_persist_the_quarantine_change() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
}

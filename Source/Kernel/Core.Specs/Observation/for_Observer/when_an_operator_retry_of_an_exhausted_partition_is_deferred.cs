// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_operator_retry_of_an_exhausted_partition_is_deferred : given.a_quarantined_partition
{
    JobState _blocker;
    PartitionRecoveryOutcome _outcome;

    void Establish()
    {
        FailedPartition.AttemptsInCurrentBudget = MaxRetryAttempts;
        _blocker = given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        HasOutstandingJobs(_blocker);
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_keep_the_existing_started_outcome() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_restore_the_quarantine() => FailedPartition.IsQuarantined.ShouldBeTrue();
    [Fact] void should_preserve_the_attempt_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(MaxRetryAttempts);
    [Fact] void should_persist_the_restored_quarantine() => _failedPartitionsStorageStats.Writes.ShouldEqual(2);
    [Fact] async Task should_drop_the_blocker() => await _jobsManager.Received(1).Delete(_blocker.Id);
    [Fact] void should_not_start_a_job_in_the_drop_turn() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}

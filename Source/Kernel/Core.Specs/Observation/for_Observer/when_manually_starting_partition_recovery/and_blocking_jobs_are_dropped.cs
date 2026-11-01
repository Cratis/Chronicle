// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery;

public class and_blocking_jobs_are_dropped : given.an_observer_with_a_failed_partition
{
    JobState _blocker;

    void Establish()
    {
        _blocker = for_Observer.given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        HasOutstandingJobs(_blocker);
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition(Partition);

    [Fact] void should_keep_the_existing_started_outcome() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_log_why_the_job_has_not_started_yet() => LoggedReason("RetryScheduled").ShouldBeTrue();
    [Fact] async Task should_drop_the_blocker() => await _jobsManager.Received(1).Delete(_blocker.Id);
    [Fact] void should_not_start_a_job_in_the_drop_turn() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());

    [Fact]
    void should_arm_the_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failure.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}

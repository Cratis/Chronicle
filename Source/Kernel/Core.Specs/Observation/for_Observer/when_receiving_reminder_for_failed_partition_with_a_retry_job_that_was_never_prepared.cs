// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A retry job stopped before it was prepared can never resume, so the resume is refused and nothing starts the
/// retry again. The job has to be dropped; nothing is started in the same call, and the next retry is scheduled.
/// </summary>
public class when_receiving_reminder_for_failed_partition_with_a_retry_job_that_was_never_prepared : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    FailedPartition _failedPartition;
    JobState _staleJob;

    void Establish()
    {
        _failedPartition = new() { Partition = (Key)Partition };
        _failedPartitionsState.Partitions = [_failedPartition];
        _staleJob = given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        HasOutstandingJobs(_staleJob);
    }

    async Task Because() => await _observer.ReceiveReminder(Partition, default);

    [Fact] async Task should_drop_the_job() => await _jobsManager.Received(1).Delete(_staleJob.Id);

    [Fact]
    void should_not_start_a_new_recover_job_in_the_same_call() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());

    [Fact]
    void should_schedule_the_next_retry() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartition.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}

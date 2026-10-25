// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A retry job stopped before it was prepared can never resume, so the resume is refused and nothing starts the
/// retry again. The job has to be dropped so a new one can take over the partition.
/// </summary>
public class when_receiving_reminder_for_failed_partition_with_a_retry_job_that_was_never_prepared : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    JobState _staleJob;

    void Establish()
    {
        _failedPartitionsState.Partitions = [new() { Partition = (Key)Partition }];
        _staleJob = given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        HasOutstandingJobs(_staleJob);
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _observer.ReceiveReminder(Partition, default);

    [Fact] async Task should_drop_the_job() => await _jobsManager.Received(1).Delete(_staleJob.Id);

    [Fact]
    void should_start_a_new_recover_job() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Key == (Key)Partition));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery;

public class and_resuming_the_job_is_refused : given.an_observer_with_a_failed_partition
{
    JobState _job;
    Exception? _error;

    void Establish()
    {
        _job = for_Observer.given.stopped_retry_jobs.Prepared(_observerKey, Partition);
        HasOutstandingJobs(_job);
        _jobsManager.Resume(_job.Id).Returns(false);
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _observer.TryStartRecoverJobForFailedPartition(Partition));

    [Fact] void should_report_an_error_instead_of_started() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_try_to_resume_the_existing_job() => await _jobsManager.Received(1).Resume(_job.Id);
    [Fact] void should_not_start_a_second_job() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}

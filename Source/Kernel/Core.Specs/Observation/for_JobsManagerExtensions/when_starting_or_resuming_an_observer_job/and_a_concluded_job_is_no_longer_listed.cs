// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A concluded job is only dangerous while it is still listed as unfinished. Once a lookup lists other jobs but not it,
/// it has finished for good and is forgotten, which keeps the remembered set bounded (Cratis/Chronicle#4548).
/// </summary>
public class and_a_concluded_job_is_no_longer_listed : given.a_jobs_manager
{
    readonly JobId _finishedJob = JobId.New();
    HashSet<JobId> _concludedJobs;
    JobState _running;

    void Establish()
    {
        _concludedJobs = [_finishedJob];
        _running = AJob(JobStatus.Running);
        HasJobs(_running);
    }

    async Task Because() => await StartOrResume(concludedJobs: _concludedJobs);

    [Fact] void should_forget_the_concluded_job() => _concludedJobs.ShouldNotContain(_finishedJob);
    [Fact] void should_report_the_running_job() => _result.ShouldEqual(_running.Id);
}

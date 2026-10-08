// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A listing can not tell a finished job from a failed lookup, so it is no evidence that a concluded job has finished.
/// Forgetting concluded jobs is left to their owner, which confirms each one on its own (Cratis/Chronicle#4548).
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

    [Fact] void should_leave_the_concluded_jobs_to_their_owner() => _concludedJobs.ShouldContain(_finishedJob);
    [Fact] void should_report_the_running_job() => _result.ShouldEqual(_running.Id);
}

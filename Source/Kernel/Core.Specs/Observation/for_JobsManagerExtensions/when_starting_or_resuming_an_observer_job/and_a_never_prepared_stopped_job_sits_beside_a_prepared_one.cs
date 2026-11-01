// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Only the stopped job that can never resume is dropped. The prepared stopped job is left alone for now: nothing is
/// resumed in the call that dropped a job, the next retry looks the jobs up afresh.
/// </summary>
public class and_a_never_prepared_stopped_job_sits_beside_a_prepared_one : given.a_jobs_manager
{
    JobState _neverPrepared;
    JobState _prepared;

    void Establish()
    {
        _neverPrepared = AJob(JobStatus.Stopped);
        _prepared = APreparedJob(JobStatus.Stopped);
        HasJobs(_neverPrepared, _prepared);
    }

    async Task Because() => await StartOrResume(maxJobsToDrop: 10);

    [Fact] async Task should_drop_the_job_that_was_never_prepared() => await _jobsManager.Received(1).Delete(_neverPrepared.Id);
    [Fact] async Task should_not_drop_the_prepared_job() => await _jobsManager.DidNotReceive().Delete(_prepared.Id);
    [Fact] async Task should_not_resume_any_job_yet() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}

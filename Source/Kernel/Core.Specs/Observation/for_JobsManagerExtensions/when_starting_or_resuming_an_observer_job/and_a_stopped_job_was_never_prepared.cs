// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job stopped before it was prepared is refused when resumed, so it blocks its work for good. When asked to, the
/// stopped job is dropped, and nothing is started in the same call: the next retry looks the jobs up afresh.
/// </summary>
public class and_a_stopped_job_was_never_prepared : given.a_jobs_manager
{
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        HasJobs(_stopped);
    }

    async Task Because() => await StartOrResume(maxJobsToDrop: 10);

    [Fact] async Task should_drop_it() => await _jobsManager.Received(1).Delete(_stopped.Id);
    [Fact] async Task should_not_try_to_resume_it() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}

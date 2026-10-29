// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A deletion is not proof the job is gone, and a failed lookup lists nothing. Once a job was dropped nothing is started
/// in the same call, even though no blocker is listed any more; the next retry looks the jobs up afresh.
/// </summary>
public class and_a_job_was_dropped_and_no_blocker_is_listed_any_more : given.a_jobs_manager
{
    void Establish()
    {
        var stopped = AJob(JobStatus.Stopped);
        HasJobs(stopped);
    }

    async Task Because() => await StartOrResume(maxJobsToDrop: 10);

    [Fact] async Task should_drop_the_job() => await _jobsManager.Received(1).Delete(Arg.Any<JobId>());
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] async Task should_not_resume_a_job() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}

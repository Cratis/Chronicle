// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Catch-up and replay do not ask for stopped jobs that can never resume to be dropped, so they keep today's behaviour:
/// the refused resume is reported and nothing is deleted or started.
/// </summary>
public class and_jobs_that_can_never_resume_are_kept_by_default : given.a_jobs_manager
{
    JobId _stoppedId;

    void Establish()
    {
        var stopped = AJob(JobStatus.Stopped);
        _stoppedId = stopped.Id;
        HasJobs(stopped);
        _jobsManager.Resume(stopped.Id).Returns(false);
    }

    async Task Because() => await StartOrResume();

    [Fact] async Task should_not_drop_it() => await _jobsManager.DidNotReceive().Delete(_stoppedId);
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
}

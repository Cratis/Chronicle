// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// Resuming is allowed to refuse - the job was never prepared, or its observer is no longer subscribed. A refused job
/// stays exactly as it was: it will not run, will not finalize, and will never report back. Reporting its id anyway
/// told the caller something was driving the work forward when nothing was, which is what left observers preparing
/// catch-up until the watchdog quarantined them. The refusal has to reach the caller as NotSet.
/// </summary>
public class and_a_stopped_job_refuses_to_resume : given.a_jobs_manager
{
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        HasJobs(_stopped);
        _jobsManager.Resume(_stopped.Id).Returns(false);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] void should_not_report_the_refused_job() => _result.ShouldNotEqual(_stopped.Id);
    [Fact] void should_tell_the_caller_the_resume_was_refused() => _onResumeRefusedWasCalled.ShouldBeTrue();
    [Fact] async Task should_not_fall_back_to_starting_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}

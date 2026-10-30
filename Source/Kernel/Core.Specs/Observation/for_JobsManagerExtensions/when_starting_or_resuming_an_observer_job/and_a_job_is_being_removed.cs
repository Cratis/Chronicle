// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job being removed is still draining and reports back when its workers stop, so it still owns its work (#5378).
/// </summary>
public class and_a_job_is_being_removed : given.a_jobs_manager
{
    JobState _removing;

    void Establish()
    {
        _removing = AJob(JobStatus.Removing);
        HasJobs(_removing);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_report_the_job_being_removed() => _result.ShouldEqual(_removing.Id);
    [Fact] void should_tell_the_caller_a_job_was_already_running() => _onAlreadyRunningJobWasCalled.ShouldBeTrue();
    [Fact] async Task should_not_resume_anything() => await _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] async Task should_not_start_a_new_job() => await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}

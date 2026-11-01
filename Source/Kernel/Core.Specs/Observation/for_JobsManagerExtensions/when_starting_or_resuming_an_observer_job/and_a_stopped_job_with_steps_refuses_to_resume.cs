// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A stopped job that did prepare steps carries work, so a refused resume - possibly a transient one - must never delete it.
/// </summary>
public class and_a_stopped_job_with_steps_refuses_to_resume : given.a_jobs_manager
{
    JobState _stopped;

    void Establish()
    {
        _stopped = AJob(JobStatus.Stopped);
        _stopped.Progress = new() { TotalSteps = 3, SuccessfulSteps = 1 };
        HasJobs(_stopped);
        _jobsManager.Resume(_stopped.Id).Returns(false);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_not_report_a_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] async Task should_not_remove_the_job() => await _jobsManager.DidNotReceive().Delete(Arg.Any<JobId>());
}

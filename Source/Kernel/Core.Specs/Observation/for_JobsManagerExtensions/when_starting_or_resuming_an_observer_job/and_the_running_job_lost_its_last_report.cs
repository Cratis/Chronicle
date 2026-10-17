// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

/// <summary>
/// A job recorded as running whose every step concluded long ago stalled finishing. Believing it made a replay report
/// that it started while nothing was created.
/// </summary>
public class and_the_running_job_lost_its_last_report : given.a_jobs_manager
{
    void Establish()
    {
        var stalled = AJob(JobStatus.Running);
        stalled.Progress = new() { TotalSteps = 4, SuccessfulSteps = 3, FailedSteps = 1 };
        stalled.StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow - TimeSpan.FromHours(1) }];
        HasJobs(stalled);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_not_report_the_stalled_job_as_running() => _onAlreadyRunningJobWasCalled.ShouldBeFalse();
    [Fact] void should_start_a_new_job() => _onStartNewWasCalled.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_starting_or_resuming_an_observer_job;

public class and_the_running_job_has_steps_in_flight : given.a_jobs_manager
{
    JobState _running;

    void Establish()
    {
        _running = AJob(JobStatus.Running);
        _running.Progress = new() { TotalSteps = 4, SuccessfulSteps = 3 };
        _running.StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow - TimeSpan.FromHours(1) }];
        HasJobs(_running);
    }

    async Task Because() => await StartOrResume();

    [Fact] void should_report_the_running_job() => _result.ShouldEqual(_running.Id);
    [Fact] void should_not_start_a_new_job() => _onStartNewWasCalled.ShouldBeFalse();
}

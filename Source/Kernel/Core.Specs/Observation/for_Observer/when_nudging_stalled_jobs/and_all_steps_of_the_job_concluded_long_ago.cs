// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_all_steps_of_the_job_concluded_long_ago : given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Progress = new() { TotalSteps = 10, SuccessfulSteps = 10 };
        _job.StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = _now - JobLiveness.StaleAfter - TimeSpan.FromMinutes(1) }];
    }

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().ResumeIfRunning(_job.Id);
}

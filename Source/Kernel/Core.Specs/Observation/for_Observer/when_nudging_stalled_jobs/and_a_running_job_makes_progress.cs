// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_running_job_makes_progress : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        _job.Progress.SuccessfulSteps = 1;
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(_job.Id);
}

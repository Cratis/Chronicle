// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_the_stalled_job_is_no_longer_running : given.an_observer_with_a_stalled_job
{
    void Establish() => ResumeAnswers(ResumeJobSuccess.JobIsNotRunning);

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_attempt_the_resume_once() => await _jobsManager.Received(1).ResumeIfRunning(_job.Id);
}

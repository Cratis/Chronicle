// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_the_resume_hook_fails : given.an_observer_with_a_stalled_job
{
    void Establish() => ResumeAnswers(Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown));

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_retry_on_the_next_tick() => await _jobsManager.Received(2).ResumeIfRunning(_job.Id);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_resume_returned_unknown_within_the_stall_period : given.an_observer_with_a_stalled_job
{
    void Establish() => ResumeAnswers(Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown));

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
        await _observer.NudgeStalledJobs();
    }

    [Fact] void should_list_on_the_next_tick_despite_the_period() => _jobsManager.Received(3).GetJobs(Arg.Any<JobQuery>());
}

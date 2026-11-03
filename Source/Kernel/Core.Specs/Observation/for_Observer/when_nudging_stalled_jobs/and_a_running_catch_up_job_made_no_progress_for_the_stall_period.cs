// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_running_catch_up_job_made_no_progress_for_the_stall_period : given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = false, RunningState = ObserverRunningState.Active };
        _stateStorage.State.CatchingUpPartitions.Add("catching-up");
    }

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_resume_the_job_once() => await _jobsManager.Received(1).ResumeIfRunning(_job.Id);
    [Fact] void should_never_call_the_unconditional_resume() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_route_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_resuming_a_stalled_job_fails : given.an_observer_with_a_stalled_job
{
    void Establish() => _jobsManager.ResumeIfRunning(_job.Id).Returns(Task.FromException<Result<ResumeJobSuccess, ResumeJobError>>(new InvalidOperationException("resume failed")));
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
        await _observer.NudgeStalledJobs();
    }

    [Fact] void should_retry_on_the_next_tick() => _jobsManager.Received(2).ResumeIfRunning(_job.Id);
    [Fact] void should_keep_the_observer_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
}

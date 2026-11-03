// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_resuming_a_stalled_job_fails : for_Observer.given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type]);
        _jobsManager.ResumeIfRunning(_job.Id).Returns(Task.FromException<Result<ResumeJobSuccess, ResumeJobError>>(new InvalidOperationException("resume failed")));
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        AdvanceClock();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_attempt_to_resume_the_job_once() => _jobsManager.Received(1).ResumeIfRunning(_job.Id);
    [Fact] void should_keep_the_observer_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
}

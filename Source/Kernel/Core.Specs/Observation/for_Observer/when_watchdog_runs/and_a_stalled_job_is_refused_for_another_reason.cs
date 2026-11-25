// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_stalled_job_is_refused_for_another_reason : for_Observer.given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type]);
        ResumeAnswers(Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.NotAdmitted));
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        AdvanceClock();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_stop_the_job() => _jobsManager.DidNotReceive().Stop(Arg.Any<JobId>());
}

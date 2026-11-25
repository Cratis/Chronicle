// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_stalled_job_can_never_be_resumed : for_Observer.given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type]);
        ResumeAnswers(Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed));
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        AdvanceClock();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_stop_the_job_so_it_no_longer_blocks_the_observer() => _jobsManager.Received().Stop(_job.Id);
}

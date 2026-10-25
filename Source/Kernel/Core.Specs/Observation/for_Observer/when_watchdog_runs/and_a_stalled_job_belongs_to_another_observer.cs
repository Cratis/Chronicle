// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_stalled_job_belongs_to_another_observer : for_Observer.given.an_observer_with_a_stalled_job
{
    void Establish() => _job.Request = new ReplayObserverRequest(_observerKey with { ObserverId = "another" }, ObserverType.Reactor, [event_type]);
    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        AdvanceClock();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().ResumeIfRunning(_job.Id);
}

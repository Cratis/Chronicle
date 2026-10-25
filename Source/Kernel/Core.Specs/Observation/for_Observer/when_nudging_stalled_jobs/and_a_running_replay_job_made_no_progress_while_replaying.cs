// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_running_replay_job_made_no_progress_while_replaying : given.an_observer_with_a_stalled_job
{
    void Establish() => _job.Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type]);
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_resume_the_job_once() => await _jobsManager.Received(1).ResumeIfRunning(_job.Id);
}

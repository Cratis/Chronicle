// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_running_replay_job_made_no_progress_while_not_replaying : given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type]);
        _stateStorage.State = _stateStorage.State with { IsReplaying = false, RunningState = ObserverRunningState.Active };
        _failedPartitionsState.Partitions = [new() { Partition = "failed" }];
    }

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().ResumeIfRunning(_job.Id);
}

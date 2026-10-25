// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_running_retry_job_for_a_failed_partition_made_no_progress : for_Observer.given.an_observer_with_a_stalled_job
{
    void Establish()
    {
        _job.Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.Reactor, "failed", EventSequenceNumber.First, [event_type]);
        _stateStorage.State = _stateStorage.State with { IsReplaying = false, RunningState = ObserverRunningState.Active };
        _failedPartitionsState.Partitions = [new() { Partition = "failed" }];
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        AdvanceClock();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_resume_the_job_once() => await _jobsManager.Received(1).Resume(_job.Id);
}

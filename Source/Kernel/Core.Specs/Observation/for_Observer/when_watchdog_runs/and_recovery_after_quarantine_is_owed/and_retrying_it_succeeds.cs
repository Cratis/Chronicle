// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.and_recovery_after_quarantine_is_owed;

public class and_retrying_it_succeeds : for_Observer.given.an_observer_with_failed_clearance_recovery
{
    ObserverRunningState _persistedStateBeforeWatchdog;

    async Task Because()
    {
        // The alert reconciliation persists the observer as disconnected, so it no longer looks quarantined.
        await ReportAlerts();
        _persistedStateBeforeWatchdog = _stateStorage.State.RunningState;

        await _observer.RunWatchdogAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_no_longer_look_quarantined_before_the_watchdog() => _persistedStateBeforeWatchdog.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_no_longer_owe_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeFalse();
    [Fact] void should_resume_the_stopped_catchup_job_once() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_retry_the_failed_partition_once() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
    [Fact] void should_subscribe_to_the_queue_once() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}

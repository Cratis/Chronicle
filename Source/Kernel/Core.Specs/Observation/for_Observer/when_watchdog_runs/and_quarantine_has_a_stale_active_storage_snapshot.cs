// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_quarantine_has_a_stale_active_storage_snapshot : for_Observer.given.an_observer_with_reloadable_state
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Active, IsReplaying = true };
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_report_authoritative_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_persisted_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_scan_replay_jobs_for_recovery() => _jobsManager.DidNotReceive().GetJobsOfType<IReplayObserver, ReplayObserverRequest>();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}

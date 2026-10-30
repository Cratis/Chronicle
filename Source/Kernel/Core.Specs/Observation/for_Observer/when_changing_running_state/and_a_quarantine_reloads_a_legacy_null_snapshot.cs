// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_quarantine_reloads_a_legacy_null_snapshot : given.an_observer_with_a_quarantine_clock
{
    ObserverState _reloaded;

    async Task Establish()
    {
        _quarantineClock.Advance(TimeSpan.FromHours(1));
        await _observer.TransitionTo<QuarantinedObserver>();
        _reloadableStateStorage.PersistedState = _stateStorage.State with { RunningState = ObserverRunningState.Active, RunningStateChangedAt = null };
    }

    async Task Because()
    {
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        _reloaded = await ReconcileAndCaptureReloadedState();
        _reloadableStateStorage.PersistedState = _stateStorage.State with { RunningState = ObserverRunningState.Active, RunningStateChangedAt = null };
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        await ReconcileSubscription();
    }

    [Fact] void should_restore_quarantine_before_followup_reads() => _reloaded.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_restore_its_entry_time_before_followup_reads() => _reloaded.RunningStateChangedAt.ShouldEqual(_quarantineTime);
    [Fact] void should_keep_the_entry_time_after_repeated_reload() => _stateStorage.State.RunningStateChangedAt.ShouldEqual(_quarantineTime);
    [Fact] void should_persist_the_original_entry_time() => _reloadableStateStorage.PersistedState.RunningStateChangedAt.ShouldEqual(_quarantineTime);
    [Fact] void should_remain_quarantined() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_wiped_quarantine_is_followed_by_a_new_episode : given.an_observer_with_a_quarantine_clock
{
    ObserverState _afterReset;

    async Task Establish()
    {
        _quarantineClock.Advance(TimeSpan.FromHours(1));
        await _observer.TransitionTo<QuarantinedObserver>();
    }

    async Task Because()
    {
        await _reloadableStateStorage.ClearStateAsync();
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        await ReconcileSubscription();
        _afterReset = _reloadableStateStorage.PersistedState;
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        await _observer.TransitionTo<QuarantinedObserver>();
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        await ReconcileSubscription();
        await _observer.SetHandledStats(42UL);
    }

    [Fact] void should_end_the_wiped_episode() => _afterReset.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_stamp_the_return_to_active() => _afterReset.RunningStateChangedAt.ShouldEqual(_quarantineTime.AddSeconds(1));
    [Fact] void should_persist_the_new_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_persist_the_new_episode_entry_after_reload_and_write() => _reloadableStateStorage.PersistedState.RunningStateChangedAt.ShouldEqual(_quarantineTime.AddSeconds(2));
}

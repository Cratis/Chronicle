// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_active_storage_is_wiped : given.an_observer_with_a_quarantine_clock
{
    async Task Because()
    {
        await _reloadableStateStorage.ClearStateAsync();
        _quarantineClock.Advance(TimeSpan.FromHours(1));
        await ReconcileSubscription();
        _quarantineClock.Advance(TimeSpan.FromSeconds(1));
        await ReconcileSubscription();
        await _observer.SetHandledStats(42UL);
    }

    [Fact] void should_persist_active_again() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_invent_history_after_the_reset() => _reloadableStateStorage.PersistedState.RunningStateChangedAt.ShouldBeNull();
    [Fact] void should_restore_the_identity() => _reloadableStateStorage.PersistedState.Identifier.ShouldEqual(_observerId);
}

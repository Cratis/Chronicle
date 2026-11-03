// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_reload_differs_from_the_last_settled_state : given.an_observer_with_a_quarantine_clock
{
    void Establish()
    {
        _reloadableStateStorage.PersistedState = _stateStorage.State with { RunningState = ObserverRunningState.Disconnected };
        _persistedRunningStateChangedAt = _quarantineTime.AddDays(-1);
    }

    async Task Because()
    {
        _quarantineClock.Advance(TimeSpan.FromHours(1));

        // Routing is Unknown; returning to the activation's previous Active state is not a new transition.
        await ReconcileSubscription();
        await _observer.SetHandledStats(42UL);
    }

    [Fact] void should_return_to_the_previous_settled_state() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_preserve_the_reloaded_timestamp_through_unknown() => _persistedRunningStateChangedAt.ShouldEqual(_quarantineTime.AddDays(-1));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_the_observer_enters_quarantine : given.an_observer_with_a_quarantine_clock
{
    void Establish() => _quarantineClock.Advance(TimeSpan.FromHours(1));

    Task Because() => _observer.TransitionTo<QuarantinedObserver>();

    [Fact] void should_be_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_the_actual_entry_time() => _stateStorage.State.RunningStateChangedAt.ShouldEqual(_quarantineTime);
    [Fact] void should_persist_the_entry_time() => _reloadableStateStorage.PersistedState.RunningStateChangedAt.ShouldEqual(_quarantineTime);
}

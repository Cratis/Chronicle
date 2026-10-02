// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_a_scheduled_transition_was_dropped : given.an_observer_with_dropped_pending_quarantine
{
    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] async Task should_clear_the_pending_only_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_keep_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}

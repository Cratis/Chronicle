// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_scheduled_quarantine_was_dropped_by_a_failed_transition : given.an_observer_with_dropped_pending_quarantine
{
    async Task Because() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_clear_the_pending_only_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_keep_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}

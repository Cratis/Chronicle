// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_automatic_reconciliation_discards_quarantine_from_wiped_storage : given.an_observer_with_reloadable_state
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        await _reloadableStateStorage.ClearStateAsync();
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    }

    async Task Because() => await ReconcileSubscription();

    [Fact] async Task should_discard_the_old_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_start_from_the_reset_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_restore_the_activation_identity() => _reloadableStateStorage.PersistedState.Identifier.ShouldEqual(_observerId);
    [Fact] async Task should_establish_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}

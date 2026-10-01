// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_observer_quarantine_after_reactivation : given.a_reactivated_quarantined_observer
{
    bool _isQuarantinedBefore;

    async Task Establish() => _isQuarantinedBefore = await _observer.IsObserverQuarantined();

    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] void should_be_quarantined_before_clearing() => _isQuarantinedBefore.ShouldBeTrue();
    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_be_disconnected_as_nobody_is_subscribed() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_leave_quarantine_in_persisted_state() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
}

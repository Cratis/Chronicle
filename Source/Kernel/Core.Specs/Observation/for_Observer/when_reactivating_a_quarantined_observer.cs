// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_reactivating_a_quarantined_observer : given.an_observer_with_subscription
{
    Observer _reactivated;

    async Task Establish() => await _observer.TransitionTo<QuarantinedObserver>();

    async Task Because() => _reactivated = await Reactivate();

    [Fact] void should_keep_running_state_quarantined_in_persisted_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_still_be_quarantined() => (await _reactivated.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_be_in_the_quarantined_state() => (await _reactivated.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_be_subscribed() => (await _reactivated.IsSubscribed()).ShouldBeFalse();
}

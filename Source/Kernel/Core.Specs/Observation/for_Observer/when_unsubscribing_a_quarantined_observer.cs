// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_unsubscribing_a_quarantined_observer : given.an_observer_with_subscription
{
    async Task Establish() => await _observer.TransitionTo<QuarantinedObserver>();

    Task Because() => _observer.Unsubscribe();

    [Fact] async Task should_be_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_keep_running_state_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_stay_in_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
}

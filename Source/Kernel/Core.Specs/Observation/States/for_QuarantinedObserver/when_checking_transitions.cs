// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_QuarantinedObserver;

public class when_checking_transitions : given.a_quarantined_observer_state
{
    bool _canTransitionToRouting;
    bool _canTransitionToDisconnected;

    async Task Because()
    {
        _canTransitionToRouting = await _state.CanTransitionTo<Routing>(_storedState);
        _canTransitionToDisconnected = await _state.CanTransitionTo<Disconnected>(_storedState);
    }

    [Fact] void should_allow_transition_to_routing() => _canTransitionToRouting.ShouldBeTrue();
    [Fact] void should_not_allow_transition_to_disconnected() => _canTransitionToDisconnected.ShouldBeFalse();
}

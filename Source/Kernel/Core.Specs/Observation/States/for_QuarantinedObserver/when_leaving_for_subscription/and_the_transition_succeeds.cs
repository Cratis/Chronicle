// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_QuarantinedObserver.when_leaving_for_subscription;

public class and_the_transition_succeeds : given.a_quarantined_observer_state
{
    bool _canTransitionToDisconnectedWhileLeaving;
    bool _canTransitionToDisconnectedAfterwards;

    void Establish() => _stateMachine
        .TransitionTo<Disconnected>()
        .Returns(async _ => _canTransitionToDisconnectedWhileLeaving = await _state.CanTransitionTo<Disconnected>(_storedState));

    async Task Because()
    {
        await _state.LeaveForSubscription();
        _canTransitionToDisconnectedAfterwards = await _state.CanTransitionTo<Disconnected>(_storedState);
    }

    [Fact] void should_transition_to_disconnected() => _stateMachine.Received(1).TransitionTo<Disconnected>();
    [Fact] void should_not_transition_to_anything_else() => _stateMachine.DidNotReceive().TransitionTo<Routing>();
    [Fact] void should_allow_transition_to_disconnected_while_leaving() => _canTransitionToDisconnectedWhileLeaving.ShouldBeTrue();
    [Fact] void should_not_allow_transition_to_disconnected_afterwards() => _canTransitionToDisconnectedAfterwards.ShouldBeFalse();
}

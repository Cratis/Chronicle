// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_QuarantinedObserver.when_leaving_for_subscription;

public class and_the_transition_fails : given.a_quarantined_observer_state
{
    Exception _error;
    bool _canTransitionToDisconnectedWhileLeaving;
    bool _canTransitionToDisconnectedAfterwards;

    void Establish() => _stateMachine
        .TransitionTo<Disconnected>()
        .Returns<Task>(async _ =>
        {
            _canTransitionToDisconnectedWhileLeaving = await _state.CanTransitionTo<Disconnected>(_storedState);
            throw new InvalidOperationException("Transition failed");
        });

    async Task Because()
    {
        _error = await Catch.Exception(() => _state.LeaveForSubscription());
        _canTransitionToDisconnectedAfterwards = await _state.CanTransitionTo<Disconnected>(_storedState);
    }

    [Fact] void should_throw_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_allow_transition_to_disconnected_while_leaving() => _canTransitionToDisconnectedWhileLeaving.ShouldBeTrue();
    [Fact] void should_not_allow_transition_to_disconnected_afterwards() => _canTransitionToDisconnectedAfterwards.ShouldBeFalse();
}

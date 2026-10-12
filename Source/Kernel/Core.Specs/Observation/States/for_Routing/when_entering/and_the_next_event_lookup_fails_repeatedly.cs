// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_next_event_lookup_fails_repeatedly : given.a_routing_state_with_a_failed_lookup
{
    async Task Because()
    {
        _resultingStoredState = await _state.OnEnter(_storedState);
        _resultingStoredState = await _state.OnEnter(_resultingStoredState);
        _resultingStoredState = await _state.OnEnter(_resultingStoredState);
    }

    [Fact] void should_leave_the_position_unchanged() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual(_storedState.NextEventSequenceNumber);
    [Fact] void should_not_start_catch_up() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_not_start_live_observing() => _stateMachine.DidNotReceive().TransitionTo<Observing>();
}

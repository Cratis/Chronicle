// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_replaying_all_events_with_no_fixed_event_types : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with { IsReplaying = true, SubscribesToAllEvents = true };
        _subscription = _subscription with { EventTypes = [] };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_transition_to_replay() => _stateMachine.Received(1).TransitionTo<Replay>();
    [Fact] void should_not_transition_to_disconnected() => _stateMachine.DidNotReceive().TransitionTo<Disconnected>();
}

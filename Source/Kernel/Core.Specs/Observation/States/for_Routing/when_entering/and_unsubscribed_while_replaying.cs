// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_unsubscribed_while_replaying : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with { IsReplaying = true };
        _subscription = ObserverSubscription.Unsubscribed;
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_transition_to_disconnected() => _stateMachine.Received(1).TransitionTo<Disconnected>();
    [Fact] void should_not_transition_to_replay() => _stateMachine.DidNotReceive().TransitionTo<Replay>();
    [Fact] void should_keep_the_pending_replay() => _resultingStoredState.IsReplaying.ShouldBeTrue();
}

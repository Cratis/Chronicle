// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.StateMachines;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_it_is_already_replaying : given.a_routing_state
{
    void Establish() => _storedState = _storedState with { IsReplaying = true };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_only_perform_one_transition() => _observer.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_transition_to_replay() => _observer.Received(1).TransitionTo<Replay>();
}

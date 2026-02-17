// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_it_is_falling_behind : given.a_routing_state
{
    void Establish()
    {
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with
        {
            Tail = 42,
            TailForEventTypes = 22
        };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_only_perform_one_transition() => _observer.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_ask_observer_to_catchup() => _observer.Received(1).CatchUp();
}

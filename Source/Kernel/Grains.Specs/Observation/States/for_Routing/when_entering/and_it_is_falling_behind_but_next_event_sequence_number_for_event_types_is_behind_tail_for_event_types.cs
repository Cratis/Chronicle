// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.when_entering;

public class and_it_is_falling_behind_but_next_event_sequence_number_for_event_types_is_behind_tail_for_event_types : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with
        {
            NextEventSequenceNumberForEventTypes = 22
        };

        _tailEventSequenceNumbers = _tailEventSequenceNumbers with
        {
            Tail = 42,
            TailForEventTypes = 21
        };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_only_perform_one_transition() => _observer.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_transition_to_catch_up() => _observer.Received(1).TransitionTo<Observing>();
    [Fact] void should_update_next_event_sequence_number_to_tail() => _resultingStoredState.NextEventSequenceNumber.Value.ShouldEqual(43UL);
}

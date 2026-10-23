// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// The last handled event is the furthest event any partition handled, not a position below which everything has
/// been handled: a partition job, or a catch-up round reporting while a live batch is handled, can move it ahead of
/// the observer's position. Events at and above the position are then still unhandled - the ones between the last
/// handled event and the tail always are - so routing must catch up from the position instead of moving it past the tail.
/// </summary>
public class and_the_last_handled_event_is_ahead_of_the_next_unhandled_event : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with
        {
            NextEventSequenceNumber = 151UL,
            LastHandledEventSequenceNumber = 200UL
        };
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with
        {
            Tail = 260UL,
            TailForEventTypes = 152UL
        };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_ask_observer_to_catch_up() => _observer.Received(1).CatchUp();
    [Fact] void should_transition_once() => _stateMachine.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_keep_the_position_at_the_first_unhandled_event() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)151UL);
}

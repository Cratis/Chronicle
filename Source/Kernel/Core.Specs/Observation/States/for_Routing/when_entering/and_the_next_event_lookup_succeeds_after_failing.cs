// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_next_event_lookup_succeeds_after_failing : given.a_routing_state_with_a_failed_lookup
{
    async Task Because()
    {
        _resultingStoredState = await _state.OnEnter(_storedState);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)10UL);
        _resultingStoredState = await _state.OnEnter(_resultingStoredState);
    }

    [Fact] void should_start_catch_up_once() => _observer.Received(1).CatchUp();
    [Fact] void should_resume_live_observing() => _stateMachine.Received(1).TransitionTo<Observing>();
    [Fact] void should_catch_up_from_the_original_position() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual(_storedState.NextEventSequenceNumber);
}

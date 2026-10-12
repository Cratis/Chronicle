// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_next_event_lookup_fails : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with { NextEventSequenceNumber = 5UL };
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with { Tail = 20UL };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_leave_the_position_unchanged() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual(_storedState.NextEventSequenceNumber);
    [Fact] void should_not_start_catch_up() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_not_start_live_observing() => _stateMachine.DidNotReceive().TransitionTo<Observing>();
}

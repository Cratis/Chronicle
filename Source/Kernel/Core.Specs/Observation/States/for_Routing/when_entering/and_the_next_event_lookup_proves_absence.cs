// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_next_event_lookup_proves_absence : given.a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with { NextEventSequenceNumber = 5UL };
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with { Tail = 20UL };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_fast_forward_past_the_tail() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual(_tailEventSequenceNumbers.Tail.Next());
    [Fact] void should_not_start_catch_up() => _observer.DidNotReceive().CatchUp();
}

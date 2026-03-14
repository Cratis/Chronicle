// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.States.for_Observing.when_entering;

public class with_events_missed_between_routing_and_subscribing : given.an_observing_state
{
    void Establish()
    {
        _storedState = _storedState with { NextEventSequenceNumber = 5UL };
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(10));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult(Result<EventSequenceNumber, GetSequenceNumberError>.Success((EventSequenceNumber)5)));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_subscribe_to_stream() => _appendedEventsQueues.Received(1).Subscribe(_observerKey, _eventTypes);
    [Fact] void should_transition_back_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}

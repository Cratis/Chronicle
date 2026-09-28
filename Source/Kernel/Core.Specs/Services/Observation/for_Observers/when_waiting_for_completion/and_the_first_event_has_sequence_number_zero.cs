// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_the_first_event_has_sequence_number_zero : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _appendedEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("a-recorded", 1), 0UL);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable }]);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 0UL,
        TailEventSequenceNumber = 0UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 0UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_evaluate_the_first_appended_event() => _eventSequence.Received(1).GetRange(0UL, 0UL, null, Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), null, null, Arg.Any<CancellationToken>());
    [Fact] void should_not_wait_for_an_excluded_first_event() => _result.IsSuccess.ShouldBeTrue();
}

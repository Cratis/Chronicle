// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_multi_event_append_starts_at_zero : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _cursor.Current.Returns(_ =>
        [
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("a-recorded", 1), 0UL),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("a-recorded", 1), 1UL)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable }]);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 0UL,
        HasFirstEventSequenceNumber = true,
        TailEventSequenceNumber = 1UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 1UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_scan_the_batch_starting_at_zero() => _eventSequence.Received(1).GetRange(0UL, 1UL, null, Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), null, null, Arg.Any<CancellationToken>());
    [Fact] void should_not_wait_for_the_excluded_batch() => _result.IsSuccess.ShouldBeTrue();
}

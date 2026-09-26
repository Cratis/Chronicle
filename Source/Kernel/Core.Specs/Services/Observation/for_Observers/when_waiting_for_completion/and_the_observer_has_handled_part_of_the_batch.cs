// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_the_observer_has_handled_part_of_the_batch : given.an_observer_with_filtered_event
{
    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = 54UL }]);
    }

    async Task Because() => await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 55UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 55UL }],
        TimeoutMilliseconds = 100
    });

    [Fact] void should_start_after_the_last_handled_event() => _eventSequence.Received(1).GetRange(55UL, 55UL, eventTypes: Arg.Any<IEnumerable<EventType>>(), tags: Arg.Any<IEnumerable<Tag>>());
}

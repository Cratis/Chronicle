// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_earlier_event_matches_the_tag_filter : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        var earlier = _appendedEvent with { Context = _appendedEvent.Context with { Tags = [new Tag("priority")] } };
        _cursor.Current.Returns(_ => [earlier, AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("a-recorded", 1), 54UL)]);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 54UL,
        FirstEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 54UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_wait_for_the_matching_event_even_if_the_tail_does_not_match() => _result.OutstandingObservers.ShouldContain("filtered-observer");
    [Fact] void should_read_only_the_unhandled_part_of_the_batch() => _eventSequence.Received(1).GetRange(53UL, 54UL, eventTypes: Arg.Any<IEnumerable<EventType>>(), tags: Arg.Any<IEnumerable<Tag>>());
}

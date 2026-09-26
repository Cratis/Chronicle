// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_event_source_type_filter_excludes_the_event : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish() => SetFilters(new Concepts.Observation.ObserverFilters([], "order"));

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_complete_without_waiting_for_the_filtered_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_narrow_the_read_to_the_source_type() => _eventSequence.Received(1).GetRange(53UL, 53UL, null, Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), (EventSourceType)"order", null, Arg.Any<CancellationToken>());
}

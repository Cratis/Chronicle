// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_explicit_default_source_type_excludes_the_append : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters([], EventSourceType.Default));
        _appendedEvent = _appendedEvent with { Context = _appendedEvent.Context with { EventSourceType = "order" } };
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_not_wait_for_the_non_default_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_check_the_appended_event() => _eventSequence.Received(1).GetRange(53UL, 53UL, null, Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), EventSourceType.Default, null, Arg.Any<CancellationToken>());
}

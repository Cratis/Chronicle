// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_the_observer_has_never_handled_an_event_in_a_non_log_sequence : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition("filtered-observer", [new EventType("a-recorded", 1)], "other-sequence", Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable }]);
        _namespaceStorage.GetEventSequence("other-sequence").Returns(_eventSequence);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = "other-sequence",
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 100
    });

    [Fact] void should_start_at_the_first_appended_event_in_that_sequence() => _eventSequence.Received(1).GetRange(53UL, 53UL, eventTypes: Arg.Any<IEnumerable<EventType>>(), tags: Arg.Any<IEnumerable<Tag>>());
    [Fact] void should_not_wait_for_filtered_out_events() => _result.IsSuccess.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_the_first_number_exceeds_the_tail : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish() => SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 54UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_not_scan_an_invalid_range() => _eventSequence.DidNotReceiveWithAnyArgs().GetRange(default!, default!, default, default, default, default, default, default);
    [Fact] void should_wait_for_the_tail_instead() => _result.OutstandingObservers.ShouldContain("filtered-observer");
}

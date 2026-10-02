// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_application_observer_is_disconnected : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _observerStateStorage.GetAll().Returns([]);
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

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_report_the_disconnected_application_observer() => _result.OutstandingObservers.ShouldContainOnly("filtered-observer");
}

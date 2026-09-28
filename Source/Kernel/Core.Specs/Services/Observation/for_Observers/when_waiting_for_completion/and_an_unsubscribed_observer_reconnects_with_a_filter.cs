// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unsubscribed_observer_reconnects_with_a_filter : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;
    int _subscriptionReads;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        var active = new ObserverSubscription(
            "filtered-observer",
            new ObserverKey("filtered-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log),
            [new EventType("a-recorded", 1)],
            typeof(IObserverSubscriber),
            SiloAddress.Zero,
            Filters: new Concepts.Observation.ObserverFilters(["priority"]));
        _observer.GetSubscription().Returns(_ => ++_subscriptionReads == 1 ? ObserverSubscription.Unsubscribed : active);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 150
    });

    [Fact] void should_recheck_the_subscription_after_reconnecting() => _subscriptionReads.ShouldBeGreaterThan(1);
    [Fact] void should_not_wait_for_an_event_excluded_by_the_new_filter() => _result.IsSuccess.ShouldBeTrue();
}

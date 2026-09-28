// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_subscription_changes_to_include_the_append : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;
    int _subscriptionReads;

    void Establish()
    {
        var key = new ObserverKey("filtered-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log);
        var eventTypes = new[] { new EventType("a-recorded", 1) };
        var excluded = new ObserverSubscription("filtered-observer", key, eventTypes, typeof(IObserverSubscriber), SiloAddress.Zero, Filters: new Concepts.Observation.ObserverFilters(["other"]));
        var included = excluded with { Filters = new Concepts.Observation.ObserverFilters(["priority"]) };
        _observer.GetSubscription().Returns(_ => ++_subscriptionReads == 1 ? excluded : included);
        _appendedEvent = _appendedEvent with { Context = _appendedEvent.Context with { Tags = [new Tag("priority")] } };
        _cursor.MoveNext().Returns(true, false, true, false);

        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition("filtered-observer", eventTypes, Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true),
            new ObserverDefinition("other-observer", eventTypes, Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
        ]);
        _observerStateStorage.GetAll().Returns(
        [
            new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = 52UL },
            new ObserverState { Identifier = "other-observer", LastHandledEventSequenceNumber = 52UL }
        ]);
        _grainFactory.GetGrain<IObserver>((string)key).Returns(_observer);
        var otherObserver = Substitute.For<IObserver>();
        otherObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>((string)new ObserverKey("other-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log)).Returns(otherObserver);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 120
    });

    [Fact] void should_wait_for_the_newly_matching_observer() => _result.OutstandingObservers.ShouldContain("filtered-observer");
    [Fact] void should_read_again_after_the_filter_changes() => _eventSequence.Received(2).GetRange(53UL, 53UL, null, Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), null, null, Arg.Any<CancellationToken>());
}

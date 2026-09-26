// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class an_observer_with_filtered_event : all_dependencies
{
    protected IObserver _observer;
    protected IEventSequenceStorage _eventSequence;
    protected IEventCursor _cursor;
    protected AppendedEvent _appendedEvent;
    protected ObserverFilters _filters;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _cursor = Substitute.For<IEventCursor>();
        _namespaceStorage.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequence);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(_observer);
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                "filtered-observer",
                [new EventType("a-recorded", 1)],
                Concepts.EventSequences.EventSequenceId.Log,
                ObserverType.Reactor,
                ObserverOwner.Client,
                true)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = 52UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
        _appendedEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("a-recorded", 1), 53UL);
        _eventSequence.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(_cursor);
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns(_ => [_appendedEvent]);
    }

    protected void SetFilters(ObserverFilters filters)
    {
        _filters = filters;
        _observer.GetSubscription().Returns(new ObserverSubscription(
            "filtered-observer",
            new ObserverKey("filtered-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log),
            [new EventType("a-recorded", 1)],
            typeof(IObserverSubscriber),
            SiloAddress.Zero,
            Filters: filters));
    }
}

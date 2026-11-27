// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_behind_a_repair_barrier : an_observer_with_skip_detection
{
    protected readonly List<EventSequenceNumber> _delivered = [];

    void Establish()
    {
        PersistStartedPartitionCatchUps();
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 14UL,
            LastHandledEventSequenceNumber = 13UL,
            PendingCatchUpRepairs = [new("P", 11UL, 14UL)]
        };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)16UL);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)16UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)14UL);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _delivered.AddRange(events.Select(_ => _.Context.SequenceNumber));
                return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
            });
    }

    protected void GivenHealthySuccessorPartition(Key partition)
    {
        var index = Substitute.For<IObserverKeyIndex>();
        index.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(_ => new Jobs.ObserverKeysForTesting([partition]));
        _eventStoreNamespaceStorage.ObserverKeyIndexes.GetFor(_observerKey, Arg.Any<ObserverReaderScope>()).Returns(index);
    }

    protected AppendedEvent Event(ulong number) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, number);

    protected async Task DeliverSuccessor()
    {
        await _subscriber.OnNext("P", [Event(14UL), Event(15UL), Event(16UL)], new ObserverSubscriberContext(null));
        await _observer.CaughtUp(JobId.NotSet, 16UL, 16UL);
    }
}

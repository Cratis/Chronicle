// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling.given;

public class an_observer_with_a_live_delivery_batch : for_Observer.given.an_observer_with_subscription_for_specific_event_type
{
    protected const int BatchSize = 101;
    protected static readonly Key Partition = "live-budget";
    protected static readonly EventSequenceNumber FirstEvent = 42UL;
    protected AppendedEvent[] _events;
    protected EventSequenceNumber _nextEventSequenceNumber;

    void Establish()
    {
        _events = Enumerable.Range(0, BatchSize)
            .Select(index => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, (ulong)FirstEvent + (ulong)index))
            .ToArray();
        _nextEventSequenceNumber = _stateStorage.State.NextEventSequenceNumber;
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(_events[^1].Context.SequenceNumber));
    }
}

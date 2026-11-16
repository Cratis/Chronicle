// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class subscription_matching : Specification
{
    protected AppendedEventsQueueObserverSubscription _subscription;
    protected AppendedEvent[] _events;
    protected IReadOnlyList<AppendedEvent> _result;

    static readonly Func<IReadOnlyList<AppendedEvent>, AppendedEventsQueueObserverSubscription, IReadOnlyList<AppendedEvent>> _filter =
        typeof(AppendedEventsQueue).GetMethod("GetFilteredEvents", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<IReadOnlyList<AppendedEvent>, AppendedEventsQueueObserverSubscription, IReadOnlyList<AppendedEvent>>>();

    protected static readonly Func<AppendedEventsQueueObserverSubscription, HashSet<EventTypeId>, bool, bool> MayMatch =
        typeof(AppendedEventsQueue).GetMethod("MayMatch", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<AppendedEventsQueueObserverSubscription, HashSet<EventTypeId>, bool, bool>>();

    void Establish()
    {
        _subscription = new(new ObserverKey("observer", "store", "namespace", "sequence"), ["matching"]);
        _events = [Event("matching", 1UL), Event("other", 2UL), Event("matching", 3UL)];
    }

    protected static AppendedEvent Event(string type, ulong sequenceNumber) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType(type, 1), sequenceNumber);

    protected void Filter() => _result = _filter(_events, _subscription);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation.given;

public class a_partition_run : Specification
{
    protected EventType _subscribedType = new("subscribed", 1);
    protected EventType _otherType = new("other", 1);
    protected AppendedEvent[] _events;
    private protected ObserverRunPreparationContext _context;
    private protected PreparedObserverRun _result;

    protected static AppendedEvent Event(EventType eventType, ulong sequenceNumber) =>
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, sequenceNumber);

    void Establish()
    {
        _events = [Event(_subscribedType, 10), Event(_otherType, 11), Event(_otherType, 12)];
        _context = new(11UL, false, new HashSet<EventTypeId> { _subscribedType.Id }, null);
    }
}

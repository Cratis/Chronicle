// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class a_storage_with_events_of_multiple_generations : an_event_sequence_storage
{
    protected static readonly EventSourceId _eventSourceId = "some-source";
    protected static readonly EventType _secondGeneration = new(_eventType.Id, 2);
    protected static readonly EventType _thirdGeneration = new(_eventType.Id, 3);
    protected static readonly EventType _otherEventType = new("other-event-type", EventTypeGeneration.First);
    protected static readonly EventType _missingEventType = new("missing-event-type", EventTypeGeneration.First);
    protected static readonly EventType _tombstoneFilter = new(_eventType.Id, EventTypeGeneration.First, true);

    async Task Establish()
    {
        await Append(0, _eventSourceId, _eventType);
        await Append(1, _eventSourceId, _otherEventType);
        await Append(2, _eventSourceId, _secondGeneration);
        await Append(3, _eventSourceId, _otherEventType);
        await Append(4, _eventSourceId, _thirdGeneration);
        await Append(5, "other-source", _secondGeneration);
        await Append(6, _eventSourceId, _otherEventType);
    }

    protected static async Task<IEnumerable<AppendedEvent>> Read(IEventCursor cursor)
    {
        using (cursor)
        {
            var events = new List<AppendedEvent>();
            while (await cursor.MoveNext())
            {
                events.AddRange(cursor.Current);
            }

            return events;
        }
    }

    Task Append(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType) =>
        _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { eventType.Generation, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
}

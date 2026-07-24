// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class appending_many_events : an_event_sequence
{
    protected List<EventToAppend> _events;

    void Establish() => _events =
    [
        EventToAppendFor("source-1"),
        EventToAppendFor("source-2"),
        EventToAppendFor("source-3")
    ];

    protected EventToAppend EventToAppendFor(EventSourceId eventSourceId) => new(
        EventSourceType.Default,
        eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        [],
        new JsonObject());

    protected IEnumerable<AppendedEvent> AppendedEventsFrom(IEnumerable<EventToAppendToStorage> events) =>
        events.Select(eventToAppend => new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                eventToAppend.EventType,
                eventToAppend.EventSourceType,
                eventToAppend.EventSourceId,
                eventToAppend.EventStreamType,
                eventToAppend.EventStreamId,
                eventToAppend.SequenceNumber,
                eventToAppend.CorrelationId),
            new ExpandoObject())).ToArray();
}

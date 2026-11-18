// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.given;

/// <summary>
/// Three events: two in one event stream (type and id) and one in another, so narrowing on each stream dimension has matches and misses.
/// </summary>
public class a_storage_with_events_of_two_event_streams : Specification
{
    protected EventSequenceStorage _storage;
    protected EventStreamType _auditStreamType;
    protected EventStreamType _otherStreamType;
    protected EventStreamId _firstStreamId;
    protected EventStreamId _secondStreamId;
    protected EventType _eventType;

    async Task Establish()
    {
        _storage = new EventSequenceStorage(
            new EventStoreName("event-store"),
            new EventStoreNamespaceName("default"),
            EventSequenceId.Log,
            new IdentityStorage());

        _auditStreamType = "Audit";
        _otherStreamType = "Other";
        _firstStreamId = "stream-one";
        _secondStreamId = "stream-two";
        _eventType = new EventType("f1e2d3c4-b5a6-4788-9900-aabbccddeeff", EventTypeGeneration.First);

        await Append(0, _auditStreamType, _firstStreamId);
        await Append(1, _otherStreamType, _secondStreamId);
        await Append(2, _auditStreamType, _secondStreamId);
    }

    protected async Task<IEnumerable<ulong>> Read(EventStreamType eventStreamType, EventStreamId eventStreamId)
    {
        var events = new List<ulong>();
        using var cursor = await _storage.GetFromSequenceNumber(
            EventSequenceNumber.First,
            EventSourceId.Unspecified,
            EventSourceType.Unspecified,
            eventStreamType,
            eventStreamId,
            []);

        while (await cursor.MoveNext())
        {
            events.AddRange(cursor.Current.Select(_ => _.Context.SequenceNumber.Value));
        }

        return events;
    }

    Task Append(ulong sequenceNumber, EventStreamType eventStreamType, EventStreamId eventStreamId) =>
        _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            "source",
            eventStreamType,
            eventStreamId,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
}

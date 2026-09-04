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
/// Six events across two event sources, two event types and three days, so that every dimension a
/// query can narrow on has both matching and non-matching events.
/// </summary>
public class a_storage_with_events_spread_over_time : Specification
{
    protected static readonly DateTimeOffset _firstDay = new(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset _secondDay = new(2026, 8, 11, 9, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset _thirdDay = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    protected EventSequenceStorage _storage;
    protected EventSourceId _firstEventSourceId;
    protected EventSourceId _secondEventSourceId;
    protected EventType _registered;
    protected EventType _archived;

    async Task Establish()
    {
        _storage = new EventSequenceStorage(
            new EventStoreName("event-store"),
            new EventStoreNamespaceName("default"),
            EventSequenceId.Log,
            new IdentityStorage());

        _firstEventSourceId = "first";
        _secondEventSourceId = "second";
        _registered = new EventType("Registered", EventTypeGeneration.First);
        _archived = new EventType("Archived", EventTypeGeneration.First);

        await Append(0, _firstEventSourceId, _registered, _firstDay, "important");
        await Append(1, _secondEventSourceId, _registered, _firstDay.AddHours(1), null);
        await Append(2, _firstEventSourceId, _archived, _secondDay, null);
        await Append(3, _secondEventSourceId, _registered, _secondDay.AddHours(2), "important");
        await Append(4, _firstEventSourceId, _registered, _thirdDay, null);
        await Append(5, _secondEventSourceId, _archived, _thirdDay.AddHours(3), null);
    }

    Task Append(ulong sequenceNumber, EventSourceId eventSourceId, EventType eventType, DateTimeOffset occurred, string? tag) =>
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
            tag is null ? [] : [new Tag(tag)],
            occurred,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
}

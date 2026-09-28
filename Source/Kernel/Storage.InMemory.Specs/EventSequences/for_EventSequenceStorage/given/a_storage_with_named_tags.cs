// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.given;

public class a_storage_with_named_tags : a_storage_with_appended_events
{
    protected static readonly DateTimeOffset _firstDay = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset _secondDay = _firstDay.AddDays(1);

    async Task Establish()
    {
        await Append(3, _firstEventSourceId, _firstDay, [new(new TagName("account"), "one"), new(new TagName("region"), "north")]);
        await Append(4, _firstEventSourceId, _firstDay, [new(new TagName("account"), "two"), new(new TagName("region"), "one")]);
        await Append(5, _firstEventSourceId, _firstDay, [new(new TagName("account"), "three"), new(new TagName("region"), "south")]);
        await Append(6, _secondEventSourceId, _secondDay, [new(new TagName("account"), "one")]);
    }

    async Task Append(ulong sequenceNumber, EventSourceId eventSourceId, DateTimeOffset occurred, IReadOnlyCollection<NamedTag> namedTags) =>
        await _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            occurred,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>(),
            null,
            namedTags);
}

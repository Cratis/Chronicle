// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_redacting_an_event_source_with_content_hash : given.an_event_sequence_storage
{
    EventSourceId _eventSourceId;
    EventEntry _stored;

    async Task Establish()
    {
        _eventSourceId = EventSourceId.New();
        await _storage.Append(
            1,
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new ExpandoObject() },
            new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = new EventHash("original-payload-hash") });
    }

    async Task Because()
    {
        await _storage.Redact(_eventSourceId, "erasure", null, CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await using var context = CreateContext();
        _stored = await context.Events.SingleAsync();
    }

    [Fact] void should_clear_the_original_content_hash() => _stored.ContentHashes.ShouldBeEmpty();
    [Fact] void should_replace_the_original_event_type() => _stored.Type.ShouldEqual(GlobalEventTypes.Redaction);
}

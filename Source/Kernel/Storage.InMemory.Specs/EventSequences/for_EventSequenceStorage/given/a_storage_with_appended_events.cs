// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.given;

public class a_storage_with_appended_events : Specification
{
    protected EventSequenceStorage _storage;
    protected EventSourceId _firstEventSourceId;
    protected EventSourceId _secondEventSourceId;
    protected EventType _eventType;

    async Task Establish()
    {
        _storage = new EventSequenceStorage(
            new EventStoreName("event-store"),
            new EventStoreNamespaceName("default"),
            EventSequenceId.Log,
            new IdentityStorage());

        _firstEventSourceId = "first";
        _secondEventSourceId = "second";
        _eventType = new EventType("d0b8f8a4-6d0d-4a1a-9a0a-1a2b3c4d5e6f", EventTypeGeneration.First);

        await Append(0, _firstEventSourceId);
        await Append(1, _secondEventSourceId);
        await Append(2, _firstEventSourceId);
    }

    Task Append(ulong sequenceNumber, EventSourceId eventSourceId) =>
        _storage.Append(
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
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
}

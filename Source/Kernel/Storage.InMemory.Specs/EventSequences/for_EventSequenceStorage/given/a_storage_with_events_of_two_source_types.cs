// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.given;

public class a_storage_with_events_of_two_source_types : Specification
{
    protected EventSequenceStorage _storage;
    protected EventSourceId _eventSourceId;
    protected EventSourceType _orderSourceType;
    protected EventSourceType _customerSourceType;
    protected EventType _eventType;

    async Task Establish()
    {
        _storage = new EventSequenceStorage(
            new EventStoreName("event-store"),
            new EventStoreNamespaceName("default"),
            EventSequenceId.Log,
            new IdentityStorage());

        _eventSourceId = "shared-identifier";
        _orderSourceType = "Order";
        _customerSourceType = "Customer";
        _eventType = new EventType("f1e2d3c4-b5a6-4788-9900-aabbccddeeff", EventTypeGeneration.First);

        await Append(0, _orderSourceType);
        await Append(1, _customerSourceType);
        await Append(2, _orderSourceType);
    }

    Task Append(ulong sequenceNumber, EventSourceType eventSourceType) =>
        _storage.Append(
            sequenceNumber,
            eventSourceType,
            _eventSourceId,
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class an_event_sequence_storage : Specification
{
    protected static readonly EventType _eventType = new("some-event-type", EventTypeGeneration.First);
    protected EventSequenceStorage _storage;

    void Establish() => _storage = new(
        "SomeEventStore",
        EventStoreNamespaceName.Default,
        EventSequenceId.Log);

    protected Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId) =>
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

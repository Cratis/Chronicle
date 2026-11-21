// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage.given;

public abstract class a_namespace_with_an_event : a_namespace_storage
{
    protected abstract EventSequenceId Sequence { get; }

    async Task Establish() => await _storage.GetEventSequence(Sequence).Append(
        EventSequenceNumber.First,
        EventSourceType.Default,
        new EventSourceId("source"),
        EventStreamType.All,
        EventStreamId.Default,
        new EventType("event-type", EventTypeGeneration.First),
        CorrelationId.New(),
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
        new Dictionary<EventTypeGeneration, EventHash>());
}

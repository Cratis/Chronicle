// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class a_storage_for_appended_generation : an_event_sequence_storage
{
    protected static IDictionary<EventTypeGeneration, ExpandoObject> Content()
    {
        dynamic original = new ExpandoObject();
        original.value = "original";
        dynamic migrated = new ExpandoObject();
        migrated.value = "migrated";
        return new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = original, [new EventTypeGeneration(2)] = migrated };
    }

    protected EventToAppendToStorage EventAt(EventSequenceNumber number, uint generation) => new(
        number,
        EventSourceType.Default,
        "some-source",
        EventStreamType.All,
        EventStreamId.Default,
        _eventType with { Generation = new EventTypeGeneration(generation) },
        CorrelationId.NotSet,
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new ExpandoObject(),
        EventHash.NotSet)
    {
        GenerationalContent = Content()
    };
}

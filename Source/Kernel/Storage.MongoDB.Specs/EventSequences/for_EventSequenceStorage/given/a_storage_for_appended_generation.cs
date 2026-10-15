// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_storage_for_appended_generation(ReplicaSetMongoDBFixture fixture) : a_replica_set_event_sequence_storage(fixture)
{
    void Establish() => _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
        .Returns(call => JsonNode.Parse(JsonSerializer.Serialize(call.Arg<ExpandoObject>()))!.AsObject());

    protected static IDictionary<EventTypeGeneration, ExpandoObject> Content()
    {
        dynamic original = new ExpandoObject();
        original.value = "original";
        dynamic migrated = new ExpandoObject();
        migrated.value = "migrated";
        return new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = original, [new EventTypeGeneration(2)] = migrated };
    }

    protected EventToAppendToStorage GenerationalEvent(EventSequenceNumber number, uint generation) => EventAt(number, _eventType with { Generation = new EventTypeGeneration(generation) }) with
    {
        GenerationalContent = Content()
    };

    protected Task<Event> Stored(EventSequenceNumber number) => _collection.Find(_ => _.SequenceNumber == number).SingleAsync();
}

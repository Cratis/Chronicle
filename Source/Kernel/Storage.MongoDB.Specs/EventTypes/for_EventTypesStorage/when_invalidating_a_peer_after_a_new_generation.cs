// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_invalidating_a_peer_after_a_new_generation(MongoDBFixture fixture)
    : given.two_event_types_storages(fixture)
{
    static readonly EventTypeId _eventTypeId = "the-event-type";
    static readonly EventTypeGeneration _secondGeneration = new(2U);

    const string FirstSchema = """{ "type": "object", "properties": { "name": { "type": "string" } } }""";
    const string SecondSchema = """{ "type": "object", "properties": { "name": { "type": "string" }, "age": { "type": "integer" } } }""";

    EventTypeDefinition _peerBeforeInvalidation;
    EventTypeDefinition _peerAfterInvalidation;

    async Task Because()
    {
        // Two storage instances over one shared collection stand in for two silos, each with its own cache.
        var firstSchema = await JsonSchema.FromJsonAsync(FirstSchema);
        await _storageA.Register(new EventType(_eventTypeId, EventTypeGeneration.First), firstSchema);

        // Warm the peer (storage B) definition cache with the single generation.
        await _storageB.GetDefinition(_eventTypeId);

        // A new generation is registered through storage A, writing to the shared collection.
        var secondSchema = await JsonSchema.FromJsonAsync(SecondSchema);
        await _storageA.Register(new EventType(_eventTypeId, _secondGeneration), secondSchema);

        // Without invalidation the peer keeps serving the stale single-generation definition.
        _peerBeforeInvalidation = await _storageB.GetDefinition(_eventTypeId);

        // The grain-service fan-out reaches silo B, which evicts its own cache.
        _storageB.Invalidate(_eventTypeId);
        _peerAfterInvalidation = await _storageB.GetDefinition(_eventTypeId);
    }

    [Fact] void should_keep_serving_the_stale_definition_before_invalidation() =>
        _peerBeforeInvalidation.Generations.Count().ShouldEqual(1);

    [Fact] void should_serve_both_generations_after_invalidation() =>
        _peerAfterInvalidation.Generations.Count().ShouldEqual(2);
}

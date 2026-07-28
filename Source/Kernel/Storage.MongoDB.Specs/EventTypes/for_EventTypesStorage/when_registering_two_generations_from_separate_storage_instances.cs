// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_two_generations_from_separate_storage_instances(MongoDBFixture fixture)
    : given.two_event_types_storages(fixture)
{
    static readonly EventTypeId _eventTypeId = "the-event-type";
    static readonly EventTypeGeneration _secondGeneration = new(2U);

    EventTypeDefinition _storedDefinition;
    BsonDocument _storedDocument;

    async Task Because()
    {
        var firstGenerationSchema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": { "name": { "type": "string" } } }
            """);
        var secondGenerationSchema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": { "name": { "type": "string" }, "age": { "type": "integer" } } }
            """);

        await _storageA.Register(new EventType(_eventTypeId, EventTypeGeneration.First), firstGenerationSchema);
        await _storageB.Register(new EventType(_eventTypeId, _secondGeneration), secondGenerationSchema);

        _storedDefinition = await _storageB.GetDefinition(_eventTypeId);
        _storedDocument = await _storedDocuments.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
    }

    [Fact] void should_store_a_single_document() => _storedDocuments.CountDocuments(Builders<BsonDocument>.Filter.Empty).ShouldEqual(1L);

    [Fact] void should_keep_the_first_generation_schema_in_the_stored_document() =>
        _storedDocument["schemas"].AsBsonDocument.Names.ShouldContain(EventTypeGeneration.First.ToString());

    [Fact] void should_keep_the_second_generation_schema_in_the_stored_document() =>
        _storedDocument["schemas"].AsBsonDocument.Names.ShouldContain(_secondGeneration.ToString());

    [Fact] void should_expose_both_generations() => _storedDefinition.Generations.Count().ShouldEqual(2);

    [Fact] void should_retain_the_first_generation_registered_by_the_first_instance() =>
        _storedDefinition.Generations.Any(_ => _.Generation == EventTypeGeneration.First).ShouldBeTrue();

    [Fact] void should_retain_the_second_generation_registered_by_the_second_instance() =>
        _storedDefinition.Generations.Any(_ => _.Generation == _secondGeneration).ShouldBeTrue();
}

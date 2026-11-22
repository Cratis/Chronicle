// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_conditionally_registering_first_generations : given.a_mocked_event_types_storage
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    async Task should_preserve_the_winner_when_the_conditional_write_loses(bool identical, bool existingType)
    {
        var winner = Winner(existingType);
        var filter = new BsonDocument();
        if (existingType)
        {
            var previous = Winner(false);
            previous.Schemas.Clear();
            previous.Schemas["1"] = BsonDocument.Parse("{}");
            _eventTypesInDatabase.Add(previous);
            _collection.UpdateOneAsync(
                Arg.Do<FilterDefinition<MongoEventType>>(value => filter = value.Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry))),
                Arg.Any<UpdateDefinition<MongoEventType>>(),
                Arg.Any<UpdateOptions>(),
                default).Returns(_ =>
                {
                    _eventTypesInDatabase.Clear();
                    _eventTypesInDatabase.Add(winner);
                    return new UpdateResult.Acknowledged(0, 0, null);
                });
        }
        else
        {
            _collection.InsertOneAsync(Arg.Any<MongoEventType>(), Arg.Any<InsertOneOptions>(), default).Returns(_ =>
            {
                _eventTypesInDatabase.Add(winner);
                return Task.FromException(for_ColdStartStorage.given.duplicate_keys.Duplicate());
            });
        }

        var generation = existingType ? new EventTypeGeneration(2) : EventTypeGeneration.First;
        var schema = await JsonSchema.FromJsonAsync(identical ? """{"type":"string"}""" : """{"type":"integer"}""");
        var registration = new EventTypeToRegister(new EventTypeDefinition(_eventTypeId, EventTypeOwner.Client, false, [new EventTypeGenerationDefinition(generation, schema)], []), EventTypeSource.Code);
        var result = await _storage.RegisterIfAbsent(registration);

        result.Changed.ShouldBeFalse();
        result.AddedGenerations.ShouldBeEmpty();
        if (identical) result.Conflict.ShouldBeNull();
        else result.Conflict.ShouldEqual(generation);
        winner.Schemas[generation.Value.ToString()].ToJson().ShouldEqual(BsonDocument.Parse("""{"type":"string"}""").ToJson());
        if (existingType)
        {
            filter.Contains("schemas").ShouldBeTrue();
            filter.Contains("migrations").ShouldBeTrue();
        }
        await _collection.DidNotReceive().BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<MongoEventType>>>(), Arg.Any<BulkWriteOptions>(), default);
    }

    MongoEventType Winner(bool existingType)
    {
        var schemas = existingType
            ? new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("{}"), ["2"] = BsonDocument.Parse("""{"type":"string"}""") }
            : new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("""{"type":"string"}""") };
        return new(_eventTypeId, EventTypeOwner.Client, EventTypeSource.Code, false, schemas, []);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_replacement_invalidates_cached_schemas : given.a_mocked_event_types_storage
{
    string _schemaAfter;
    string _definitionAfter;

    async Task Establish()
    {
        _eventTypesInDatabase.Add(new(_eventTypeId, EventTypeOwner.Server, EventTypeSource.Code, true, new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("""{"deprecated":false,"type":"object"}""") }));
        await _storage.GetDefinition(_eventTypeId);
        await _storage.GetFor(_eventTypeId, _firstGeneration);
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<MongoEventType>>(), Arg.Any<UpdateDefinition<MongoEventType>>(), Arg.Any<UpdateOptions>(), default)
            .Returns(call =>
            {
                var update = call.Arg<UpdateDefinition<MongoEventType>>().Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry));
                _eventTypesInDatabase[0].Schemas["1"] = update["$set"]["schemas.1"].AsBsonDocument;
                return new UpdateResult.Acknowledged(1, 1, null);
            });
    }

    async Task Because()
    {
        await _storage.ReplaceSchema(_eventTypeId, _firstGeneration, await JsonSchema.FromJsonAsync("""{"type":"object"}"""));
        _schemaAfter = (await _storage.GetFor(_eventTypeId, _firstGeneration)).Schema.ToJson();
        _definitionAfter = (await _storage.GetDefinition(_eventTypeId)).Generations.Single().Schema.ToJson();
    }

    [Fact] void should_refresh_cached_schema() => _schemaAfter.ShouldNotContain("deprecated");
    [Fact] void should_refresh_cached_definition() => _definitionAfter.ShouldNotContain("deprecated");
    [Fact] void should_preserve_owner() => _eventTypesInDatabase[0].Owner.ShouldEqual(EventTypeOwner.Server);
    [Fact] void should_preserve_tombstone() => _eventTypesInDatabase[0].Tombstone.ShouldBeTrue();
}

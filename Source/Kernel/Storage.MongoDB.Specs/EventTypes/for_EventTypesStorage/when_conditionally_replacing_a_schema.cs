// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_conditionally_replacing_a_schema : given.a_mocked_event_types_storage
{
    BsonDocument _filter;
    BsonDocument _update;
    bool _won;
    bool _stale;

    void Establish() => _collection.UpdateOneAsync(
        Arg.Do<FilterDefinition<MongoEventType>>(filter => _filter = filter.Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry))),
        Arg.Do<UpdateDefinition<MongoEventType>>(update => _update = update.Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry)).AsBsonDocument),
        Arg.Any<UpdateOptions>(),
        default)
        .Returns(new UpdateResult.Acknowledged(1, 1, null), new UpdateResult.Acknowledged(0, 0, null));

    async Task Because()
    {
        var expected = await JsonSchema.FromJsonAsync("""{"type":"object"}""");
        var replacement = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"added":{"type":"string"}}}""");
        _won = await _storage.TryReplaceSchema(_eventTypeId, 1, expected, replacement);
        _stale = await _storage.TryReplaceSchema(_eventTypeId, 1, expected, replacement);
    }

    [Fact] void should_report_the_winning_write() => _won.ShouldBeTrue();
    [Fact] void should_reject_the_stale_write() => _stale.ShouldBeFalse();
    [Fact] void should_compare_the_stored_schema_atomically() => _filter["schemas.1"]["type"].AsString.ShouldEqual("object");
    [Fact] void should_compare_the_event_type() => _filter["_id"].AsString.ShouldEqual(_eventTypeId.Value);
    [Fact] void should_update_only_the_target_generation() => _update["$set"].AsBsonDocument.ElementCount.ShouldEqual(1);
}

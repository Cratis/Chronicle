// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_replacing_an_existing_schema : given.a_mocked_event_types_storage
{
    BsonDocument _filter;
    BsonDocument _update;
    UpdateOptions _options;
    bool _changed;
    bool _unchanged;

    void Establish() => _collection.UpdateOneAsync(
        Arg.Do<FilterDefinition<MongoEventType>>(filter => _filter = filter.Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry))),
        Arg.Do<UpdateDefinition<MongoEventType>>(update => _update = update.Render(new RenderArgs<MongoEventType>(BsonSerializer.LookupSerializer<MongoEventType>(), BsonSerializer.SerializerRegistry)).AsBsonDocument),
        Arg.Do<UpdateOptions>(options => _options = options),
        default)
        .Returns(new UpdateResult.Acknowledged(1, 1, null), new UpdateResult.Acknowledged(0, 0, null));

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"title":"Keep","type":"object"}""");
        _changed = await _storage.ReplaceSchema(_eventTypeId, _firstGeneration, schema);
        _unchanged = await _storage.ReplaceSchema(_eventTypeId, _firstGeneration, schema);
    }

    [Fact] void should_report_database_mutation() => _changed.ShouldBeTrue();
    [Fact] void should_not_report_unchanged_or_missing_schema() => _unchanged.ShouldBeFalse();
    [Fact] void should_require_existing_generation() => _filter["schemas.1"]["$exists"].AsBoolean.ShouldBeTrue();
    [Fact] void should_require_event_type() => _filter["_id"].AsString.ShouldEqual(_eventTypeId.Value);
    [Fact] void should_only_update_one_schema() => _update["$set"].AsBsonDocument.ElementCount.ShouldEqual(1);
    [Fact] void should_preserve_title() => _update["$set"]["schemas.1"]["title"].AsString.ShouldEqual("Keep");
    [Fact] void should_not_upsert() => (_options?.IsUpsert ?? false).ShouldBeFalse();
}

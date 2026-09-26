// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_registering_a_generation_with_only_a_nested_title_changed : given.a_mocked_event_types_storage
{
    const string StoredSchema = """{"type":"object","properties":{"key":{"type":"object","title":"OldKey","properties":{"id":{"type":"string"}}}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"key":{"type":"object","title":"NewKey","properties":{"id":{"type":"string"}}}}}""";
    bool _mutated;

    void Establish() => _eventTypesInDatabase.Add(new MongoEventType(
        _eventTypeId,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        false,
        new Dictionary<string, BsonDocument> { [_firstGeneration.ToString()] = BsonDocument.Parse(StoredSchema) }));

    async Task Because() => _mutated = await _storage.Register(
        new EventType(_eventTypeId, _firstGeneration),
        await JsonSchema.FromJsonAsync(IncomingSchema));

    [Fact] void should_not_report_a_mutation() => _mutated.ShouldBeFalse();
    [Fact] void should_not_write_a_document() => _collection.DidNotReceiveWithAnyArgs().UpdateOneAsync(default, default, default, default);
}

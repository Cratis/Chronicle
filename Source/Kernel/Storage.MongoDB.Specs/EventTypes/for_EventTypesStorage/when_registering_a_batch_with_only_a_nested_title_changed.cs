// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_registering_a_batch_with_only_a_nested_title_changed : given.a_mocked_event_types_storage
{
    const string StoredSchema = """{"type":"object","properties":{"value":{"type":"object","title":"OldKey","properties":{"name":{"type":"string"}}}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"value":{"type":"object","title":"NewKey","properties":{"name":{"type":"string"}}}}}""";
    IEnumerable<EventTypeId> _mutated;

    void Establish() => _eventTypesInDatabase.Add(new MongoEventType(
        _eventTypeId,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        false,
        new Dictionary<string, BsonDocument> { [_firstGeneration.ToString()] = BsonDocument.Parse(StoredSchema) }));

    async Task Because() => _mutated = await _storage.Register([new EventTypeToRegister(
        new EventTypeDefinition(
            _eventTypeId,
            EventTypeOwner.Client,
            false,
            [new EventTypeGenerationDefinition(_firstGeneration, await JsonSchema.FromJsonAsync(IncomingSchema))],
            []),
        EventTypeSource.Code)]);

    [Fact] void should_not_report_a_mutation() => _mutated.ShouldBeEmpty();
    [Fact] void should_not_write_a_document() => _collection.DidNotReceiveWithAnyArgs().BulkWriteAsync(default, default, default);
    [Fact] void should_keep_the_stored_schema() => _eventTypesInDatabase.Single().Schemas[_firstGeneration.ToString()].ToJson().ShouldContain("OldKey");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_registering_a_batch_with_a_changed_schema : given.a_mocked_event_types_storage
{
    const string StoredSchema = """{"type":"object","properties":{"old":{"type":"string"}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"new":{"type":"string"}}}""";
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

    [Fact] void should_report_the_mutation() => _mutated.ShouldContainOnly(_eventTypeId);
    [Fact] void should_write_the_document() => _collection.ReceivedWithAnyArgs(1).BulkWriteAsync(default, default, default);
}

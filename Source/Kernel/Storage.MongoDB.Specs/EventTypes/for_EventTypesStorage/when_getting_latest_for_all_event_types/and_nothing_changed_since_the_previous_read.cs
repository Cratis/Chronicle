// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_latest_for_all_event_types;

public class and_nothing_changed_since_the_previous_read : given.a_mocked_event_types_storage
{
    const string Schema = """{ "type": "object", "properties": { "name": { "type": "string" } } }""";

    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    void Establish() => _eventTypesInDatabase.Add(Stored());

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];
        _eventTypesInDatabase[0] = Stored();
        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_the_same_schema_instance() => ReferenceEquals(_first[0], _second[0]).ShouldBeTrue();
    [Fact] void should_return_the_same_json_schema_instance() => ReferenceEquals(_first[0].Schema, _second[0].Schema).ShouldBeTrue();

    static MongoEventType Stored() => new(
        _eventTypeId,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        false,
        new Dictionary<string, BsonDocument> { { _firstGeneration.ToString(), BsonDocument.Parse(Schema) } });
}

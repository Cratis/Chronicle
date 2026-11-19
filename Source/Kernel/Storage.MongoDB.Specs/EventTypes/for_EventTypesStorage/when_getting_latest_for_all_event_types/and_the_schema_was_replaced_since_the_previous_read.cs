// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_latest_for_all_event_types;

public class and_the_schema_was_replaced_since_the_previous_read : given.a_mocked_event_types_storage
{
    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    void Establish() => _eventTypesInDatabase.Add(Stored("name"));

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];
        _eventTypesInDatabase[0] = Stored("title");
        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_the_replaced_schema() => _second[0].Schema.Properties.ContainsKey("title").ShouldBeTrue();
    [Fact] void should_not_return_the_previous_schema() => _second[0].Schema.Properties.ContainsKey("name").ShouldBeFalse();
    [Fact] void should_leave_the_schema_read_before_as_it_was() => _first[0].Schema.Properties.ContainsKey("name").ShouldBeTrue();

    static MongoEventType Stored(string property) => new(
        _eventTypeId,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        false,
        new Dictionary<string, BsonDocument>
        {
            { _firstGeneration.ToString(), new BsonDocument { { "type", "object" }, { "properties", new BsonDocument(property, new BsonDocument("type", "string")) } } }
        });
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_latest_for_all_event_types;

public class and_a_generation_was_added_since_the_previous_read : given.a_mocked_event_types_storage
{
    EventTypeSchema[] _second;

    void Establish() => _eventTypesInDatabase.Add(Stored(_firstGeneration));

    async Task Because()
    {
        await _storage.GetLatestForAllEventTypes();
        _eventTypesInDatabase[0] = Stored(_firstGeneration, _secondGeneration);
        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_the_added_generation() => _second[0].Type.Generation.ShouldEqual(_secondGeneration);

    static MongoEventType Stored(params EventTypeGeneration[] generations) => new(
        _eventTypeId,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        false,
        generations.ToDictionary(_ => _.ToString(), _ => new BsonDocument("type", "object")));
}

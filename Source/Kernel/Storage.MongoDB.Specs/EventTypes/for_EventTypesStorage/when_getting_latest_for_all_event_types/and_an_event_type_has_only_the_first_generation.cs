// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_latest_for_all_event_types;

public class and_an_event_type_has_only_the_first_generation : given.a_mocked_event_types_storage
{
    EventTypeSchema[] _result;

    void Establish() =>
        _eventTypesInDatabase.Add(new MongoEventType(
            _eventTypeId,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            false,
            new Dictionary<string, BsonDocument>
            {
                { _firstGeneration.ToString(), new BsonDocument("type", "object") }
            }));

    async Task Because() => _result = [.. await _storage.GetLatestForAllEventTypes()];

    [Fact] void should_return_one_schema_for_the_event_type() => _result.Length.ShouldEqual(1);
    [Fact] void should_return_the_first_generation() => _result[0].Type.Generation.ShouldEqual(_firstGeneration);
}

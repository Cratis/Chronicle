// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class an_event_type_with_two_generations_in_the_database : a_mocked_event_types_storage
{
    protected const string FirstGenerationProperty = "firstGenerationValue";
    protected const string SecondGenerationProperty = "secondGenerationValue";

    void Establish() =>
        _eventTypesInDatabase.Add(new MongoEventType(
            _eventTypeId,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            false,
            new Dictionary<string, BsonDocument>
            {
                { _firstGeneration.ToString(), SchemaFor(FirstGenerationProperty) },
                { _secondGeneration.ToString(), SchemaFor(SecondGenerationProperty) }
            }));

    static BsonDocument SchemaFor(string propertyName) => new()
    {
        { "type", "object" },
        { "properties", new BsonDocument(propertyName, new BsonDocument("type", "string")) }
    };
}

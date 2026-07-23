// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.for_EventTypesStorage.when_checking_has_for;

public class and_event_type_has_only_the_first_generation : given.an_event_types_storage
{
    bool _hasFirstGeneration;
    bool _hasSecondGeneration;

    void Establish() =>
        _eventTypesInDatabase.Add(new EventType(
            _eventTypeId,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            false,
            new Dictionary<string, BsonDocument>
            {
                { _firstGeneration.ToString(), new BsonDocument("type", "object") }
            }));

    async Task Because()
    {
        _hasFirstGeneration = await _storage.HasFor(_eventTypeId, _firstGeneration);
        _hasSecondGeneration = await _storage.HasFor(_eventTypeId, _secondGeneration);
    }

    [Fact] void should_have_the_first_generation() => _hasFirstGeneration.ShouldBeTrue();
    [Fact] void should_not_have_the_second_generation() => _hasSecondGeneration.ShouldBeFalse();
}

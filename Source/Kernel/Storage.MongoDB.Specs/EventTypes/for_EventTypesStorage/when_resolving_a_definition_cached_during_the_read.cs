// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_resolving_a_definition_cached_during_the_read : given.a_read_completed_after_caching
{
    EventTypeDefinition _cached;
    EventTypeDefinition _result;

    async Task Because()
    {
        var pending = _storage.GetDefinition(_eventTypeId);
        _cached = await _storage.GetDefinition(_eventTypeId);
        CompleteRead(_eventTypesInDatabase[0] with
        {
            Schemas = new Dictionary<string, BsonDocument> { { "invalid-generation", new BsonDocument("type", "object") } }
        });
        _result = await pending;
    }

    [Fact] void should_reuse_the_cached_definition_without_converting_the_stale_document() => _result.ShouldBeSame(_cached);
}

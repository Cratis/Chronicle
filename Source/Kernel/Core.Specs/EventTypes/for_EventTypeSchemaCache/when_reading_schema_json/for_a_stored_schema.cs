// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_reading_schema_json;

public class for_a_stored_schema : given.a_schema_cache
{
    string _first;
    string _second;

    void Establish()
    {
        var registered = new EventTypeSchema(
            new EventType(_eventTypeId, _firstGeneration),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\"}"));
        _eventTypesStorage.GetFor(_eventTypeId, _firstGeneration).Returns(Task.FromResult(registered));
    }

    void Because()
    {
        _first = _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);
        _second = _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);
    }

    [Fact] void should_return_the_schema_as_json() => _first.ShouldContain("\"type\"");
    [Fact] void should_read_the_schema_from_the_event_store_asked_for() => _storage.Received().GetEventStore(_eventStore);
    [Fact] void should_serialize_a_schema_instance_only_once() => ReferenceEquals(_first, _second).ShouldBeTrue();
}

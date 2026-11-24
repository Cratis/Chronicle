// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class after_the_registered_schema_changed : given.a_serializer_for_appended_events
{
    string _before;
    string _after;

    void Establish() => SchemaLookupReturns();

    void Because()
    {
        _before = _schemaCache.GetSchemaJsonFor(_eventStore, _eventTypeId, EventTypeGeneration.First);

        // The registry hands out a new schema instance once the event type has changed.
        _schema = new EventTypeSchema(
            new EventType(_eventTypeId, EventTypeGeneration.First),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\",\"description\":\"changed\"}"));
        _after = _schemaCache.GetSchemaJsonFor(_eventStore, _eventTypeId, EventTypeGeneration.First);
    }

    [Fact] void should_not_have_the_change_before_it_happened() => _before.ShouldNotContain("changed");
    [Fact] void should_serve_the_text_of_the_changed_schema() => _after.ShouldContain("changed");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class multiple_events_of_the_same_type : given.a_serializer_for_appended_events
{
    void Establish() => SchemaLookupReturns();

    string _first;
    string _last;

    void Because()
    {
        Serialize(AnEvent());
        _first = _schemaCache.GetSchemaJsonFor(_eventStore, _eventTypeId, EventTypeGeneration.First);
        Serialize(AnEvent());
        Serialize(AnEvent());
        _last = _schemaCache.GetSchemaJsonFor(_eventStore, _eventTypeId, EventTypeGeneration.First);
    }

    // Looking the schema up is the registry's business and it answers without storage once warm; what the serializer
    // owes is to render the schema text once per schema instance rather than once per event.
    [Fact] void should_render_the_schema_text_once() => ReferenceEquals(_first, _last).ShouldBeTrue();
}

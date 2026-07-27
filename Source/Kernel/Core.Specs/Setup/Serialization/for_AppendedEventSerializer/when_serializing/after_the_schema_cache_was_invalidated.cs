// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class after_the_schema_cache_was_invalidated : given.a_serializer_for_appended_events
{
    void Establish() => SchemaLookupReturns();

    void Because()
    {
        Serialize(AnEvent());
        _schemaCache.Invalidate(_eventStore, _eventTypeId);
        Serialize(AnEvent());
    }

    [Fact] void should_look_the_schema_up_again() => SchemaLookups().ShouldEqual(2);
}

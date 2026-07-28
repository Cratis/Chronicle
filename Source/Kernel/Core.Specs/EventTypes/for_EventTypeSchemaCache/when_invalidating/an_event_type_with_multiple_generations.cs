// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class an_event_type_with_multiple_generations : given.a_schema_cache
{
    void Establish()
    {
        _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);
        _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _secondGeneration);
    }

    void Because()
    {
        _cache.Invalidate(_eventStore, _eventTypeId);
        _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);
        _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _secondGeneration);
    }

    [Fact] void should_resolve_every_generation_again() => SchemaLookups().ShouldEqual(4);
}

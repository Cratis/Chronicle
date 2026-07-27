// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class a_different_event_type : given.a_schema_cache
{
    void Establish() => _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);

    void Because()
    {
        _cache.Invalidate(_eventStore, _otherEventTypeId);
        _cache.GetSchemaJsonFor(_eventStore, _eventTypeId, _firstGeneration);
    }

    [Fact] void should_keep_serving_the_cached_schema() => SchemaLookups().ShouldEqual(1);
}

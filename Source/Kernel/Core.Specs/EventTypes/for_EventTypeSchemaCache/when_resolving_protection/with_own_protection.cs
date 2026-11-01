// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class with_own_protection : given.a_protection_schema_cache
{
    JsonSchema _result;

    async Task Because() => _result = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _secondGeneration, _higher);

    [Fact] void should_preserve_the_own_schema_instance_when_nothing_is_inherited() => ReferenceEquals(_higher, _result).ShouldBeTrue();
}

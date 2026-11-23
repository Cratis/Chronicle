// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class without_inherited_metadata : given.a_protection_schema_cache
{
    JsonSchema _callerSchema;
    JsonSchema _result;

    async Task Establish()
    {
        _definition = _definition with { Generations = [new(_firstGeneration, _own)] };
        _callerSchema = await JsonSchema.FromJsonAsync(_own.ToJson());
    }

    async Task Because() => _result = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _callerSchema);

    [Fact] void should_use_the_owning_schema_from_the_definition_snapshot() => ReferenceEquals(_own, _result).ShouldBeTrue();
}

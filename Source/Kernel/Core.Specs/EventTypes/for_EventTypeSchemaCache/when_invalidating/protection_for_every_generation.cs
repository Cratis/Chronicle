// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class protection_for_every_generation : given.a_protection_schema_cache
{
    JsonSchema _first;
    JsonSchema _second;

    async Task Establish()
    {
        await Resolve();
        await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _secondGeneration, _higher);
        _definition = _definition with { Generations = [new(_firstGeneration, _own), new(_secondGeneration, JsonSchema.FromJson(_own.ToJson()))] };
    }

    async Task Because()
    {
        _cache.Invalidate(_eventStore, _eventTypeId);
        _first = await Resolve();
        var ownSecond = _definition.Generations.Single(_ => _.Generation == _secondGeneration).Schema;
        _second = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _secondGeneration, ownSecond);
    }

    [Fact] void should_discard_inherited_protection() => _first.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_reload_the_changed_own_generation() => _second.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_reload_every_generation() => DefinitionLookups().ShouldEqual(4);
}

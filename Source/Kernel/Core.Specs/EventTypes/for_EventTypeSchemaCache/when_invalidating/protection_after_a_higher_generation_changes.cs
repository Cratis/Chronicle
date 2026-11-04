// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class protection_after_a_higher_generation_changes : given.a_protection_schema_cache
{
    JsonSchema _before;
    JsonSchema _after;

    async Task Establish()
    {
        _definition = _definition with { Generations = [new(_firstGeneration, _own)] };
        _before = await Resolve();
        _definition = _definition with { Generations = [new(_firstGeneration, _own), new(_secondGeneration, _higher)] };
    }

    async Task Because()
    {
        _cache.Invalidate(_eventStore, _eventTypeId);
        _after = await Resolve();
    }

    [Fact] void should_discard_the_negative_decision() => _before.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_inherit_the_new_classification() => _after.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_reload_the_definition() => DefinitionLookups().ShouldEqual(2);
    [Fact] void should_not_mutate_the_old_schema() => _own.HasSchemaMetadata().ShouldBeFalse();
}

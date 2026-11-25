// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class after_the_definition_changes : given.a_protection_schema_cache
{
    JsonSchema _before;
    JsonSchema _after;

    async Task Establish() => _before = await Resolve();

    async Task Because()
    {
        _definition = _definition with { Generations = [new(_firstGeneration, _own)] };
        _after = await Resolve();
    }

    [Fact] void should_have_inherited_protection_before() => _before.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_follow_the_new_definition_without_any_invalidation() => _after.HasSchemaMetadata().ShouldBeFalse();
}

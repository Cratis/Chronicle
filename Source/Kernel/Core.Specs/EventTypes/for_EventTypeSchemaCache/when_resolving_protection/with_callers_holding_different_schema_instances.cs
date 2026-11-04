// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class with_callers_holding_different_schema_instances : given.a_protection_schema_cache
{
    JsonSchema _retained;
    JsonSchema _fromRetained;
    JsonSchema _fromCurrent;
    long _allocations;

    async Task Establish()
    {
        _retained = await JsonSchema.FromJsonAsync(_own.ToJson());
        await Resolve();
        await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _retained);
    }

    async Task Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            _fromCurrent = await Resolve();
            _fromRetained = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _retained);
        }

        _allocations = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_return_the_same_effective_schema_to_both() => ReferenceEquals(_fromCurrent, _fromRetained).ShouldBeTrue();
    [Fact] void should_inherit_protection_for_both() => _fromRetained.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_only_load_the_definition_once() => DefinitionLookups().ShouldEqual(1);
    [Fact] void should_allocate_nothing_when_alternating() => _allocations.ShouldEqual(0);
}

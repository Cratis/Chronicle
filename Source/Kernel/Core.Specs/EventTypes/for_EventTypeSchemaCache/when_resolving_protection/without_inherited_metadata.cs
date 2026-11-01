// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class without_inherited_metadata : given.a_protection_schema_cache
{
    JsonSchema _callerSchema;
    JsonSchema _result;
    long _allocations;

    void Establish()
    {
        _definition = _definition with { Generations = [new(_firstGeneration, _own)] };
        _callerSchema = JsonSchema.FromJson(_own.ToJson());
    }

    async Task Because()
    {
        _result = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _callerSchema);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _callerSchema).GetAwaiter().GetResult();
        }

        _allocations = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_preserve_the_callers_exact_schema_instance() => ReferenceEquals(_callerSchema, _result).ShouldBeTrue();
    [Fact] void should_allocate_nothing_for_a_warm_negative_decision() => _allocations.ShouldEqual(0);
}

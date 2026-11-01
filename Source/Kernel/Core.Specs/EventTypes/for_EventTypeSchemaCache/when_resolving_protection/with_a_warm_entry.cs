// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class with_a_warm_entry : given.a_protection_schema_cache
{
    JsonSchema _effective;
    JsonSchema _again;
    long _allocations;

    async Task Establish()
    {
        _effective = await Resolve();
        await Resolve();
    }

    async Task Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            _again = await Resolve();
        }

        _allocations = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_reuse_the_immutable_effective_schema() => ReferenceEquals(_effective, _again).ShouldBeTrue();
    [Fact] void should_inherit_protection() => _effective.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_not_change_the_registered_schema() => _own.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_only_load_the_definition_once() => DefinitionLookups().ShouldEqual(1);
    [Fact] void should_allocate_nothing_on_the_warm_decision_path() => _allocations.ShouldEqual(0);
}

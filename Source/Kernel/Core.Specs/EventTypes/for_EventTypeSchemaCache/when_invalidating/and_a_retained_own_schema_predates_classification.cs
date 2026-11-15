// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class and_a_retained_own_schema_predates_classification : given.a_protection_schema_cache
{
    JsonSchema _currentOwn;
    JsonSchema _result;

    async Task Establish()
    {
        await Resolve();
        _currentOwn = await JsonSchema.FromJsonAsync(_definition.Generations.Last().Schema.ToJson());
        _definition = _definition with { Generations = [new(_firstGeneration, _currentOwn)] };
    }

    async Task Because()
    {
        _cache.Invalidate(_eventStore, _eventTypeId);
        _result = await Resolve();
    }

    [Fact] void should_return_the_current_own_schema_not_the_retained_plain_hint() => ReferenceEquals(_currentOwn, _result).ShouldBeTrue();
    [Fact] void should_keep_protection_when_no_metadata_needs_inheriting() => _result.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_leave_the_retained_schema_unchanged() => _own.HasSchemaMetadata().ShouldBeFalse();
}

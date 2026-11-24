// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class with_inherited_protection : given.a_protection_schema_cache
{
    JsonSchema _callerSchema;
    JsonSchema _fromCaller;
    JsonSchema _fromRegistered;

    async Task Establish() => _callerSchema = await JsonSchema.FromJsonAsync(_own.ToJson());

    async Task Because()
    {
        _fromRegistered = await Resolve();
        _fromCaller = await _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _callerSchema);
    }

    [Fact] void should_inherit_protection() => _fromRegistered.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_not_change_the_registered_schema() => _own.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_not_let_the_callers_own_schema_decide_the_result() => ReferenceEquals(_fromRegistered, _fromCaller).ShouldBeTrue();
}

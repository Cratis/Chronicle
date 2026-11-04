// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_invalidating;

public class protection_while_a_cold_load_is_pending : given.a_protection_schema_cache
{
    readonly TaskCompletionSource<EventTypeDefinition> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventTypeDefinition _old;
    Task<JsonSchema> _load;
    JsonSchema _result;

    void Establish()
    {
        _old = _definition with { Generations = [new(_firstGeneration, _own)] };
        _eventTypesStorage.GetDefinition(_eventTypeId).Returns(_pending.Task, Task.FromResult(_definition));
        _load = Resolve().AsTask();
    }

    async Task Because()
    {
        _cache.Invalidate(_eventStore, _eventTypeId);
        _pending.SetResult(_old);
        _result = await _load;
    }

    [Fact] void should_not_publish_the_stale_negative_decision() => _result.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_retry_against_the_new_definition() => DefinitionLookups().ShouldEqual(2);
    [Fact] async Task should_cache_only_the_current_decision() => ReferenceEquals(_result, await Resolve()).ShouldBeTrue();
}

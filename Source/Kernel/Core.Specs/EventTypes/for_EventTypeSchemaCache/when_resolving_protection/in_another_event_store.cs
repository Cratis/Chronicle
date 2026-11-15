// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class in_another_event_store : given.a_protection_schema_cache
{
    readonly EventStoreName _otherStore = "other-store";
    JsonSchema _result;

    async Task Establish()
    {
        await Resolve();
        var otherTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(_otherStore).EventTypes.Returns(otherTypes);
        otherTypes.GetDefinition(_eventTypeId).Returns(_definition with { Generations = [new(_firstGeneration, _own)] });
    }

    async Task Because() => _result = await _cache.GetProtectionSchemaFor(_otherStore, _eventTypeId, _firstGeneration, _own);

    [Fact] void should_not_reuse_another_stores_classification() => _result.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_preserve_the_unclassified_schema_instance() => ReferenceEquals(_result, _own).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Events.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas;

public class when_the_store_is_empty : given.stored_schemas
{
    IEnumerable<EventTypesWithAnnotations> _report;
    NormalizeEventTypeSchemasResult _result;

    void Establish() => _selected.EventTypes.Returns(new EventTypesStorage());

    async Task Because()
    {
        _report = await EventTypesWithAnnotations.GetEventTypesWithAnnotations(_store, _storage);
        _result = await new NormalizeEventTypeSchemas(_store).Handle(_storage, _cache);
    }

    [Fact] void should_report_no_annotations() => _report.ShouldBeEmpty();
    [Fact] void should_change_no_generations() => _result.Changed.ShouldBeEmpty();
    [Fact] void should_not_evict_caches() => _cache.ReceivedCalls().ShouldBeEmpty();
}

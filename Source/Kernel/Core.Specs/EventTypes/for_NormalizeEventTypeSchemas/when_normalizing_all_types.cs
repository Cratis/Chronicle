// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas;

public class when_normalizing_all_types : given.stored_schemas
{
    IEnumerable<EventTypesWithAnnotations> _changed;
    IEnumerable<EventTypesWithAnnotations> _report;

    async Task Because()
    {
        _changed = (await new NormalizeEventTypeSchemas(_store, []).Handle(_storage, _cache)).Changed;
        _report = await EventTypesWithAnnotations.GetEventTypesWithAnnotations(_store, _storage);
    }

    [Fact] void should_change_all_affected_generations() => _changed.Count().ShouldEqual(3);
    [Fact] void should_leave_no_reportable_annotations() => _report.ShouldBeEmpty();
    [Fact] void should_not_evict_clean_types() => _cache.DidNotReceive().Invalidate(_store, "clean");
    [Fact] void should_evict_each_changed_type() => _cache.Received(1).Invalidate(_store, "unselected");
}

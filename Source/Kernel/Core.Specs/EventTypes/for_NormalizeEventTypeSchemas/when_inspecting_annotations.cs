// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas;

public class when_inspecting_annotations : given.stored_schemas
{
    IEnumerable<EventTypesWithAnnotations> _report;

    async Task Because() => _report = await EventTypesWithAnnotations.GetEventTypesWithAnnotations(_store, _storage);

    [Fact] void should_report_all_affected_generations() => _report.Count().ShouldEqual(3);
    [Fact] void should_omit_clean_generations() => _report.Any(item => item.EventTypeId == "clean").ShouldBeFalse();
    [Fact] void should_report_distinct_sorted_keywords() => _report.First().Keywords.ShouldEqual(["contentEncoding", "deprecated"]);
    [Fact] void should_not_evict_caches() => _cache.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_rewrite_schemas() => JsonSchemaAnnotations.FindAnnotationKeywords((await _eventTypes.GetFor("affected", 1)).Schema.Node).Count.ShouldEqual(2);
}

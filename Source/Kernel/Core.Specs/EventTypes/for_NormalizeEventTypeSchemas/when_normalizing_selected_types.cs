// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas;

public class when_normalizing_selected_types : given.stored_schemas
{
    IEnumerable<EventTypesWithAnnotations> _changed;
    IEnumerable<EventTypesWithAnnotations> _secondRun;

    async Task Because()
    {
        var command = new NormalizeEventTypeSchemas(_store, ["affected", "missing"]);
        _changed = (await command.Handle(_storage, _cache)).Changed;
        _secondRun = (await command.Handle(_storage, _cache)).Changed;
    }

    [Fact] void should_report_both_changed_generations() => _changed.Count().ShouldEqual(2);
    [Fact] void should_report_the_removed_keywords() => _changed.First().Keywords.ShouldEqual(["contentEncoding", "deprecated"]);
    [Fact] void should_be_idempotent() => _secondRun.ShouldBeEmpty();
    [Fact] void should_not_access_event_storage() => _selected.ReceivedCalls().Select(call => call.GetMethodInfo().Name).Distinct().ShouldEqual(["get_EventTypes"]);
    [Fact] async Task should_keep_generation_count() => (await _eventTypes.GetDefinition("affected")).Generations.Count().ShouldEqual(2);
    [Fact] async Task should_match_client_shaped_schema_exactly() => JsonNode.DeepEquals((await _eventTypes.GetFor("affected", 1)).Schema.Node, JsonNode.Parse("""{"title":"Useful","description":"Keep","properties":{"data":{"type":"string"},"literal":{"default":{"deprecated":true}}}}""")).ShouldBeTrue();
    [Fact] void should_evict_once_per_changed_type() => _cache.Received(1).Invalidate(_store, "affected");
    [Fact] void should_not_evict_unselected_types() => _cache.DidNotReceive().Invalidate(_store, "unselected");
    [Fact] async Task should_retain_unselected_schemas() => JsonSchemaAnnotations.FindAnnotationKeywords((await _eventTypes.GetFor("unselected", 1)).Schema.Node).Count.ShouldEqual(2);
    [Fact] async Task should_retain_other_stores() => JsonSchemaAnnotations.FindAnnotationKeywords((await _otherStore.GetFor("affected", 1)).Schema.Node).Count.ShouldEqual(2);
    [Fact] async Task should_retain_titles() => (await _eventTypes.GetFor("affected", 1)).Schema.Node["title"].GetValue<string>().ShouldEqual("Useful");
    [Fact] async Task should_retain_literal_data() => (await _eventTypes.GetFor("affected", 1)).Schema.Node["properties"]["literal"]["default"]["deprecated"].GetValue<bool>().ShouldBeTrue();
    [Fact] async Task should_not_create_missing_types() => (await _eventTypes.GetAllDefinitions()).Any(item => item.Id == "missing").ShouldBeFalse();
}

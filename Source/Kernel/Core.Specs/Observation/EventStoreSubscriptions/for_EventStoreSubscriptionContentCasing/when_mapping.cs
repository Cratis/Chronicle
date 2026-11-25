// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionContentCasing;

public class when_mapping : Specification
{
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "FlowType": { "type": "string" },
            "Token": { "type": "string" },
            "Nested": { "type": "object", "properties": { "InnerValue": { "type": "integer" } } },
            "Items": { "type": "array", "items": { "type": "object", "properties": { "ItemName": { "type": "string" } } } },
            "Key": { "type": "string" },
            "KEY": { "type": "string" },
            "Both": { "type": "string" }
          }
        }
        """;

    JsonSchema _schema;
    JsonObject _casingMismatch;
    JsonObject _nested;
    JsonObject _exact;
    JsonObject _ambiguousSchema;
    JsonObject _ambiguousContent;
    JsonObject _unknown;

    void Establish()
    {
        _schema = JsonSchema.FromJson(SchemaJson);
        _casingMismatch = (JsonObject)JsonNode.Parse("""{"flowType":"login","TOKEN":"abc"}""")!;
        _nested = (JsonObject)JsonNode.Parse("""{"nested":{"innerValue":1},"items":[{"itemName":"x"},{"itemName":"y"}]}""")!;
        _exact = (JsonObject)JsonNode.Parse("""{"FlowType":"login","Token":"abc"}""")!;
        _ambiguousSchema = (JsonObject)JsonNode.Parse("""{"key":"one"}""")!;
        _ambiguousContent = (JsonObject)JsonNode.Parse("""{"both":"one","BOTH":"two"}""")!;
        _unknown = (JsonObject)JsonNode.Parse("""{"other":"value"}""")!;
    }

    void Because()
    {
        EventStoreSubscriptionContentCasing.Map(_casingMismatch, _schema);
        EventStoreSubscriptionContentCasing.Map(_nested, _schema);
        EventStoreSubscriptionContentCasing.Map(_exact, _schema);
        EventStoreSubscriptionContentCasing.Map(_ambiguousSchema, _schema);
        EventStoreSubscriptionContentCasing.Map(_ambiguousContent, _schema);
        EventStoreSubscriptionContentCasing.Map(_unknown, _schema);
    }

    [Fact] void should_rename_properties_to_the_schema_casing() => _casingMismatch.Select(_ => _.Key).ShouldEqual(["FlowType", "Token"]);
    [Fact] void should_keep_the_values_when_renaming() => _casingMismatch["FlowType"]!.GetValue<string>().ShouldEqual("login");
    [Fact] void should_rename_nested_object_properties() => _nested["Nested"]!.AsObject().ContainsKey("InnerValue").ShouldBeTrue();
    [Fact] void should_rename_properties_of_array_items() => _nested["Items"]!.AsArray().All(_ => _!.AsObject().ContainsKey("ItemName")).ShouldBeTrue();
    [Fact] void should_leave_exact_matches_untouched() => _exact.Select(_ => _.Key).ShouldEqual(["FlowType", "Token"]);
    [Fact] void should_not_rename_when_the_schema_has_several_candidates() => _ambiguousSchema.Select(_ => _.Key).ShouldEqual(["key"]);
    [Fact] void should_not_rename_when_the_content_collides_in_casing() => _ambiguousContent.Select(_ => _.Key).ShouldEqual(["both", "BOTH"]);
    [Fact] void should_leave_unknown_properties_untouched() => _unknown.Select(_ => _.Key).ShouldEqual(["other"]);
}

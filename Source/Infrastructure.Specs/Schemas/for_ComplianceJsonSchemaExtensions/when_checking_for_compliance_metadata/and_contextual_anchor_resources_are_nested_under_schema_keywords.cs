// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_contextual_anchor_resources_are_nested_under_schema_keywords : Specification
{
    readonly List<JsonSchemaProperty> _properties = [];
    readonly List<bool> _classified = [];

    void Establish()
    {
        string[] direct = ["if", "then", "else", "not", "contains", "additionalProperties", "unevaluatedProperties", "unevaluatedItems", "propertyNames", "items", "additionalItems", "contentSchema"];
        string[] maps = ["properties", "patternProperties", "dependentSchemas", "dependencies", "$defs", "definitions"];
        string[] arrays = ["allOf", "anyOf", "oneOf", "prefixItems", "items"];
        foreach (var keyword in direct) Add(keyword, Protected());
        foreach (var keyword in maps) Add(keyword, new JsonObject { ["nested"] = Protected() });
        foreach (var keyword in arrays) Add(keyword, new JsonArray(Protected()));
    }

    void Because()
    {
        foreach (var property in _properties)
        {
            _classified.Add(property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII") && ConverterSchemas.HasProtectedValueMetadata(property));
        }
    }

    [Fact] void should_exercise_every_supported_schema_keyword() => _classified.Count.ShouldEqual(23);
    [Fact] void should_find_all_matching_resources() => _classified.TrueForAll(value => value).ShouldBeTrue();

    static JsonObject Protected() => new()
    {
        ["$id"] = "candidate",
        ["$dynamicAnchor"] = "personal",
        ["compliance"] = new JsonArray(new JsonObject { ["metadataType"] = "PII", ["details"] = "" })
    };

    void Add(string keyword, JsonNode child)
    {
        var schema = new JsonSchema(new JsonObject
        {
            ["$id"] = "https://schemas.example/document",
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$dynamicRef"] = "#personal" } },
            ["$defs"] = new JsonObject
            {
                ["base"] = new JsonObject { ["$dynamicAnchor"] = "personal", ["type"] = "string" },
                ["container"] = new JsonObject { [keyword] = child }
            }
        });
        _properties.Add(schema.Properties["value"]);
    }
}

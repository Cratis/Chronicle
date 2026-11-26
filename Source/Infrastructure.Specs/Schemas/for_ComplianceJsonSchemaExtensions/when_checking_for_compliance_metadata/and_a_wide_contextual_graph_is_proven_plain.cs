// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_wide_contextual_graph_is_proven_plain : Specification
{
    JsonSchemaProperty _property;
    bool _protected;
    JsonSchema.MetadataReach _reach;

    void Establish()
    {
        var definitions = new JsonObject();
        for (var index = 0; index < 96; index++)
        {
            definitions[$"candidate{index}"] = new JsonObject
            {
                ["$id"] = $"candidate{index}", ["$dynamicAnchor"] = "value", ["$dynamicRef"] = "#value", ["type"] = "string",
                ["$ref"] = "candidate0#/allOf/0", ["allOf"] = new JsonArray(new JsonObject { ["$ref"] = "#value" })
            };
        }
        var schema = new JsonSchema(new JsonObject
        {
            ["$id"] = "https://schemas.example/document",
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$ref"] = "candidate0" } },
            ["$defs"] = definitions
        });
        _property = schema.Properties["value"];
        _ = _property.Reference;
    }

    void Because()
    {
        _protected = ConverterSchemas.HasProtectedValueMetadata(_property);
        _reach = _property.MetadataReferences;
    }

    [Fact] void should_not_confuse_route_fanout_with_indeterminate_protection() => _protected.ShouldBeFalse();
    [Fact] void should_visit_each_schema_once() => _reach.UniqueNodes.ShouldEqual(193);
    [Fact] void should_expand_the_matching_anchor_group_once() => _reach.AnchorExpansions.ShouldEqual(1);
    [Fact] void should_prove_the_graph_complete() => _reach.Indeterminate.ShouldBeFalse();
}

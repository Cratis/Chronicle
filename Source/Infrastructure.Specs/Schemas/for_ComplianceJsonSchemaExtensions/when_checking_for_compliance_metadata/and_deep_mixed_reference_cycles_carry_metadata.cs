// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_deep_mixed_reference_cycles_carry_metadata : Specification
{
    JsonSchemaProperty _property;
    JsonSchema.MetadataReach _reach;

    void Establish()
    {
        var definitions = new JsonObject();
        for (var index = 0; index < 160; index++)
        {
            definitions[$"node{index}"] = new JsonObject { ["$ref"] = $"#/$defs/node{index + 1}" };
        }
        definitions["node160"] = JsonNode.Parse("""
            {"anyOf":[{"$ref":"#/$defs/node0"},{"type":"string","compliance":[{"metadataType":"PII","details":""}]}]}
            """);
        var schema = new JsonSchema(new JsonObject
        {
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$dynamicRef"] = "#/$defs/node0" } },
            ["$defs"] = definitions
        });
        _property = schema.Properties["value"];
    }

    void Because() => _reach = _property.MetadataReferences;

    [Fact] void should_reach_the_marker_beyond_the_old_depth_limit() => _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_preserve_conversion_protection() => ConverterSchemas.HasProtectedValueMetadata(_property).ShouldBeTrue();
    [Fact] void should_visit_nodes_rather_than_cyclic_routes() => _reach.UniqueNodes.ShouldEqual(164);
    [Fact] void should_complete_without_depth_truncation() => _reach.Indeterminate.ShouldBeFalse();
}

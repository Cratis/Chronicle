// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_deeply_nested_anchor_resources_are_indexed : Specification
{
    JsonSchemaProperty _property;
    bool _found;

    void Establish()
    {
        JsonObject nested = new()
        {
            ["$id"] = "candidate", ["$dynamicAnchor"] = "personal", ["type"] = "string",
            ["compliance"] = new JsonArray(new JsonObject { ["metadataType"] = "PII", ["details"] = "" })
        };
        for (var index = 0; index < 160; index++) nested = new JsonObject { ["if"] = nested };
        var schema = new JsonSchema(new JsonObject
        {
            ["$id"] = "https://schemas.example/document",
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$dynamicRef"] = "#personal" } },
            ["$defs"] = new JsonObject
            {
                ["base"] = new JsonObject { ["$dynamicAnchor"] = "personal", ["type"] = "string" },
                ["nested"] = nested
            }
        });
        _property = schema.Properties["value"];
    }

    void Because() => _found = _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII");

    [Fact] void should_index_resources_beyond_the_old_recursive_depth_limit() => _found.ShouldBeTrue();
    [Fact] void should_finish_indexing_without_uncertainty() => _property.MetadataReferences.Indeterminate.ShouldBeFalse();
}

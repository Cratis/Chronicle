// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class when_embedded_resource_pointers_conflict : Specification
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    void should_match_public_reference_resolution_without_warming_the_reference(bool hasEmbeddedTarget, bool hasContextualSibling)
    {
        var node = JsonNode.Parse("""
            {"$id":"https://example.test/root","$defs":{
              "personal":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "embedded":{"$id":"https://example.test/embedded","$defs":{"personal":{"type":"string"}},
                "properties":{"value":{"$ref":"#/$defs/personal"}}}
            }}
            """);
        if (!hasEmbeddedTarget) node["$defs"]["embedded"]["$defs"].AsObject().Remove("personal");
        if (hasContextualSibling)
        {
            node["$defs"]["plain"] = new JsonObject { ["type"] = "string" };
            node["properties"] = new JsonObject { ["context"] = new JsonObject { ["$dynamicRef"] = "#/$defs/plain" } };
        }
        var schema = new JsonSchema(node.AsObject());
        var embedded = schema.Definitions["embedded"];
        var property = embedded.Properties["value"];
        var metadata = property.HasComplianceMetadata();
        metadata.ShouldEqual(!hasEmbeddedTarget);
        property.Reference.HasComplianceMetadata().ShouldEqual(metadata);
    }
}

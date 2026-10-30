// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SchemaMetadataExtensions;

public class when_detecting_resource_metadata
{
    [Theory]
    [InlineData("personal", "$id", "personal")]
    [InlineData("#Person", "$anchor", "Person")]
    public void should_detect_metadata_on_the_referenced_resource(string reference, string declaration, string name)
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{}},"$defs":{"member":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        schema.Node["properties"]!["value"]!["$ref"] = reference;
        schema.Node["$defs"]!["member"]![declaration] = name;
        schema.HasSchemaMetadata().ShouldBeTrue();
        schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
        schema.HasSchemaMetadata(SchemaMetadataCategory.Security).ShouldBeFalse();
    }

    [Fact]
    public void should_preserve_metadata_on_the_root_pointer_despite_a_foreign_anchor()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"person":{"$ref":"#Person"}},"Person":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"$defs":{"foreign":{"$id":"foreign","$anchor":"Person","type":"string"}}}
            """);
        schema.HasSchemaMetadata().ShouldBeTrue();
    }

    [Fact]
    public void should_not_detect_a_foreign_anchor_as_root_metadata()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{"$ref":"#Person"}},"$defs":{"foreign":{"$id":"foreign","$anchor":"Person","type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        schema.HasSchemaMetadata().ShouldBeFalse();
    }
}

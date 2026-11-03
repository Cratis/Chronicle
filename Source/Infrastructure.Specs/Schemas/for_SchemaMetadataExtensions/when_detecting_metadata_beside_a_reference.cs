// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SchemaMetadataExtensions;

public class when_detecting_metadata_beside_a_reference : Specification
{
    JsonSchema _schema;

    void Establish() => _schema = JsonSchema.FromJson("""
        {"type":"object","properties":{"unprotected":{"$ref":"#/$defs/text"},"protected":{"$ref":"#/$defs/text","compliance":[{"metadataType":"PII","details":""}]}},"$defs":{"text":{"type":"string"}}}
        """);

    [Fact] void should_detect_metadata_on_the_reference() => _schema.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_detect_matching_metadata_on_the_reference() => _schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_detect_other_metadata_types() => _schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "other").ShouldBeFalse();
    [Fact] void should_not_detect_other_categories() => _schema.HasSchemaMetadata(SchemaMetadataCategory.Security).ShouldBeFalse();
}

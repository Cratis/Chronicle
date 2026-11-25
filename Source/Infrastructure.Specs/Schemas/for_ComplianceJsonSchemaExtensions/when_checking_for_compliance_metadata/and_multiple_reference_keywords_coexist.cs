// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_multiple_reference_keywords_coexist : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _property;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$ref":"#/$defs/plain","$dynamicRef":"#personal","$recursiveRef":"#"}},
          "$defs":{
            "plain":{"type":"string"},
            "personal":{"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}
          }
        }
        """);

    void Because() => _property = _schema.Properties["value"];

    [Fact] void should_not_skip_the_dynamic_edge_after_the_static_edge() => _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_keep_deterministic_type_resolution_static_first() => _property.ActualSchema.Type.ShouldEqual(JsonObjectType.String);
}

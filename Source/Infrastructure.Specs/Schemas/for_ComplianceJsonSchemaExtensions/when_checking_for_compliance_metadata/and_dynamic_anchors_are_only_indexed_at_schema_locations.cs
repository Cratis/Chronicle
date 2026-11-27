// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_dynamic_anchors_are_only_indexed_at_schema_locations : Specification
{
    JsonSchema _schema;
    bool _protected;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$dynamicRef":"#personal"}},
          "$defs":{"plain":{"$dynamicAnchor":"personal","type":"string"}},
          "default":{"$dynamicAnchor":"personal","compliance":[{"metadataType":"PII","details":""}]},
          "const":{"$dynamicAnchor":"personal","security":[{"metadataType":"Secret","details":""}]},
          "enum":[{"$dynamicAnchor":"personal","compliance":[{"metadataType":"PII","details":""}]}],
          "examples":[{"$dynamicAnchor":"personal","security":[{"metadataType":"Secret","details":""}]}],
          "x-extension":{"$dynamicAnchor":"personal","compliance":[{"metadataType":"PII","details":""}]}
        }
        """);

    void Because() => _protected = _schema.Properties["value"].HasSchemaMetadata();

    [Fact] void should_ignore_anchors_inside_instance_data_and_unknown_annotations() => _protected.ShouldBeFalse();
}

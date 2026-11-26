// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_a_cyclic_composed_scalar_has_a_concrete_pii_handler : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$dynamicRef":"#value"}},
          "$defs":{"base":{"$dynamicAnchor":"value","allOf":[{"$ref":"#/$defs/base"}],"anyOf":[{"type":"integer"},{"type":"null"}],"compliance":[{"metadataType":"PII","details":""}]}}
        }
        """);

    void Because() => _metadata = _manager.MetadataFor(_schema.Properties["value"], forComparison: true);

    [Fact] void should_find_the_real_annotation_without_recursive_strict_type_inference() => _metadata.Length.ShouldEqual(1);
    [Fact] void should_keep_its_category() => _metadata[0].Category.ShouldEqual(SchemaMetadataCategory.Compliance);
    [Fact] void should_keep_its_type() => _metadata[0].Metadata.metadataType.ShouldEqual("PII");
}

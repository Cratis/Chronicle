// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_a_recursive_reference_selects_a_protected_scalar : given.real_pii_protection
{
    JsonSchemaProperty _property;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "$recursiveAnchor":true,"type":"string","compliance":[{"metadataType":"PII","details":""}],
              "properties":{"value":{"$recursiveRef":"#"}}
            }
            """);
        _property = _schema.Properties["value"];
    }

    void Because() => _metadata = _manager.MetadataFor(_property, forComparison: false);

    [Fact] void should_collect_the_recursive_target_metadata() => _metadata.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_metadata_type() => _metadata[0].Metadata.metadataType.ShouldEqual("PII");
}

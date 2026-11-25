// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_contextual_references_do_not_invent_or_merge_handlers : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _comparison;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _unresolved;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{
            "value":{"$dynamicRef":"#personal","compliance":[{"metadataType":"PII","details":""}]},
            "missing":{"$recursiveRef":"https://unavailable.example/schema"}
          },
          "$defs":{"personal":{"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}],"security":[{"metadataType":"PII","details":""}]}}
        }
        """);

    void Because()
    {
        _comparison = _manager.MetadataFor(_schema.Properties["value"], forComparison: true);
        _unresolved = _manager.MetadataFor(_schema.Properties["missing"], forComparison: true);
    }

    [Fact] void should_deduplicate_metadata_within_the_same_category() => _comparison.Count(entry => entry.Category == SchemaMetadataCategory.Compliance).ShouldEqual(1);
    [Fact] void should_keep_the_other_category_distinct() => _comparison.Count(entry => entry.Category == SchemaMetadataCategory.Security).ShouldEqual(1);
    [Fact] void should_not_manufacture_a_handler_for_an_unresolved_reference() => _unresolved.Length.ShouldEqual(0);
}

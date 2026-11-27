// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_a_dynamic_reference_selects_a_protected_scalar : given.real_pii_protection
{
    JsonSchemaProperty _property;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _again;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "properties":{"value":{"$dynamicRef":"#personal"}},
              "$defs":{"personal":{"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}}
            }
            """);
        _property = _schema.Properties["value"];
    }

    async Task Because()
    {
        _metadata = _manager.MetadataFor(_property, forComparison: false);
        _again = _manager.MetadataFor(_property, forComparison: false);
        _plaintext = new() { ["value"] = "sensitive" };
        await Protect();
    }

    [Fact] void should_collect_the_reachable_handler_metadata() => _metadata.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_category() => _metadata[0].Category.ShouldEqual(SchemaMetadataCategory.Compliance);
    [Fact] void should_preserve_the_metadata_type() => _metadata[0].Metadata.metadataType.ShouldEqual("PII");
    [Fact] void should_not_cache_a_context_dependent_result() => ReferenceEquals(_metadata, _again).ShouldBeFalse();
    [Fact] void should_encrypt_the_value_on_append() => _protected["value"].GetValue<string>().ShouldNotEqual("sensitive");
}

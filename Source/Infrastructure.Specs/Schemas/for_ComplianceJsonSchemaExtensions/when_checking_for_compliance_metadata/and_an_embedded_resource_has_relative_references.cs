// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_an_embedded_resource_has_relative_references : Specification
{
    JsonSchema _schema;
    JsonSchema _resource;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://example.invalid/root",
          "properties":{"value":{"$ref":"https://example.invalid/a/"}},
          "$defs":{
            "resource":{"$id":"https://example.invalid/a/","properties":{
              "direct":{"$ref":"sibling"},
              "array":{"type":"array","items":{"$ref":"sibling"}},
              "dictionary":{"additionalProperties":{"$ref":"sibling"}},
              "all":{"allOf":[{"$ref":"sibling"}]},
              "pointer":{"$ref":"#/properties/direct"},
              "anchor":{"$ref":"#Personal"},
              "plain":{"$ref":"plain"}
            },"$defs":{"anchored":{"$anchor":"Personal","$ref":"sibling"}}},
            "sibling":{"$id":"https://example.invalid/a/sibling","type":"string","compliance":[{"metadataType":"PII","details":""}]},
            "plain":{"$id":"https://example.invalid/a/plain","type":"string"}
          }
        }
        """);

    void Because() => _resource = _schema.Properties["value"].Reference!;

    [Fact] void should_detect_metadata_from_the_root() => _schema.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_a_property() => _resource.Properties["direct"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_items() => _resource.Properties["array"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_additional_properties() => _resource.Properties["dictionary"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_all_of() => _resource.Properties["all"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_a_resource_pointer() => _resource.Properties["pointer"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_metadata_through_a_resource_anchor() => _resource.Properties["anchor"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_not_classify_a_plain_sibling_as_personal() => _resource.Properties["plain"].HasComplianceMetadata().ShouldBeFalse();
}

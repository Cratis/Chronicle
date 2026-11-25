// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_cloned_child_schemas_inherit_dynamic_scope : Specification
{
    JsonSchema _schema;
    JsonSchema _inner;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://schemas.example/document","$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}],
          "properties":{"entered":{"$ref":"inner"}},
          "$defs":{
            "inner":{
              "$id":"inner","properties":{
                "value":{"$dynamicRef":"base#personal"},
                "list":{"type":"array","items":{"$dynamicRef":"base#personal"}},
                "map":{"type":"object","additionalProperties":{"$dynamicRef":"base#personal"}}
              },
              "$defs":{"local":{"$dynamicRef":"base#personal"}},
              "allOf":[{"$dynamicRef":"base#personal"}],
              "anyOf":[{"$dynamicRef":"base#personal"}],
              "oneOf":[{"$dynamicRef":"base#personal"}]
            },
            "base":{"$id":"base","$dynamicAnchor":"personal","type":"string"}
          }
        }
        """);

    void Because() => _inner = _schema.Properties["entered"].Reference;

    [Fact] void should_preserve_scope_for_properties() => _inner.Properties["value"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_items() => _inner.Properties["list"].Item.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_additional_properties() => _inner.Properties["map"].AdditionalPropertiesSchema.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_definitions() => _inner.Definitions["local"].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_all_of() => _inner.AllOf[0].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_any_of() => _inner.AnyOf[0].HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_scope_for_one_of() => _inner.OneOf[0].HasComplianceMetadata().ShouldBeTrue();
}

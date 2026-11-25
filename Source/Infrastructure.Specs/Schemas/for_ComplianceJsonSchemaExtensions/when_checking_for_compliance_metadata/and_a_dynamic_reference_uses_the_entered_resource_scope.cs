// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_dynamic_reference_uses_the_entered_resource_scope : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _entered;
    JsonSchemaProperty _unrelated;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://schemas.example/document",
          "properties":{"entered":{"$ref":"outer"},"direct":{"$ref":"base"}},
          "$defs":{
            "outer":{"$id":"outer","$dynamicAnchor":"personal","compliance":[{"metadataType":"PII","details":""}],"$ref":"base"},
            "base":{"$id":"base","$dynamicAnchor":"personal","type":"object","properties":{"value":{"$dynamicRef":"#personal"}}},
            "unrelated":{"$id":"unrelated","$dynamicAnchor":"personal","security":[{"metadataType":"Secret","details":""}]}
          }
        }
        """);

    void Because()
    {
        _entered = _schema.Properties["entered"].Reference.Reference.Properties["value"];
        _unrelated = _schema.Properties["direct"].Reference.Properties["value"];
    }

    [Fact] void should_include_the_protected_outer_binding() => _entered.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_find_the_outer_metadata_type() => _entered.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_protect_conversion() => ConverterSchemas.HasProtectedValueMetadata(_entered).ShouldBeTrue();
    [Fact] void should_not_enter_an_unrelated_resource() => _unrelated.HasSecurityMetadata().ShouldBeFalse();
    [Fact] void should_not_reuse_a_different_route_classification() => _unrelated.HasComplianceMetadata().ShouldBeFalse();
}

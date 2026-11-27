// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_recursive_reference_uses_eligible_outer_roots : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _eligible;
    JsonSchemaProperty _ineligible;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://schemas.example/document",
          "properties":{"eligible":{"$ref":"outer"},"ineligible":{"$ref":"stopped"}},
          "$defs":{
            "outer":{"$id":"outer","$recursiveAnchor":true,"compliance":[{"metadataType":"PII","details":""}],"$ref":"base"},
            "stopped":{"$id":"stopped","$recursiveAnchor":true,"compliance":[{"metadataType":"PII","details":""}],"$ref":"barrier"},
            "barrier":{"$id":"barrier","$recursiveAnchor":false,"type":"object","properties":{"value":{"$recursiveRef":"#"}}},
            "base":{"$id":"base","$recursiveAnchor":true,"type":"object","properties":{"value":{"$recursiveRef":"#"}}}
          }
        }
        """);

    void Because()
    {
        _eligible = _schema.Properties["eligible"].Reference.Reference.Properties["value"];
        _ineligible = _schema.Properties["ineligible"].Reference.Reference.Properties["value"];
    }

    [Fact] void should_include_an_eligible_outer_root() => _eligible.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_find_its_concrete_metadata() => _eligible.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_rebind_when_the_initial_root_is_not_recursive() => _ineligible.HasComplianceMetadata().ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_dynamic_and_ordinary_anchors_collide : Specification
{
    JsonSchemaProperty _dynamicFirst;
    JsonSchemaProperty _ordinaryFirst;

    void Because()
    {
        _dynamicFirst = JsonSchema.FromJson("""
            {
              "properties":{"value":{"$dynamicRef":"#personal"}},
              "$defs":{
                "dynamic":{"$dynamicAnchor":"personal","type":"string"},
                "ordinary":{"$anchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }
            }
            """).Properties["value"];
        _ordinaryFirst = JsonSchema.FromJson("""
            {
              "properties":{"value":{"$dynamicRef":"#personal"}},
              "$defs":{
                "ordinary":{"$anchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "dynamic":{"$dynamicAnchor":"personal","type":"string"}
              }
            }
            """).Properties["value"];
    }

    [Fact] void should_not_classify_dynamic_first_as_unprotected() => _dynamicFirst.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_not_classify_ordinary_first_as_unprotected() => _ordinaryFirst.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_conservatively_classify_personal_data_with_dynamic_first() => _dynamicFirst.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_conservatively_classify_personal_data_with_ordinary_first() => _ordinaryFirst.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_invent_a_handler_for_the_ambiguous_plain_target() => _dynamicFirst.GetComplianceMetadata().ShouldBeEmpty();
    [Fact] void should_not_invent_security_for_dynamic_first() => _dynamicFirst.HasSchemaMetadata(SchemaMetadataCategory.Security, _ => true).ShouldBeFalse();
    [Fact] void should_not_invent_security_for_ordinary_first() => _ordinaryFirst.HasSchemaMetadata(SchemaMetadataCategory.Security, _ => true).ShouldBeFalse();
}

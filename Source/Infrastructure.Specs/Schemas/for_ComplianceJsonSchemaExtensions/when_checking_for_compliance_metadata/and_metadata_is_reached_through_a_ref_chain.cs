// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_metadata_is_reached_through_a_ref_chain : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _terminal;
    JsonSchemaProperty _intermediate;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"terminal":{"$ref":"#/$defs/A"},"intermediate":{"$ref":"#/$defs/D"},"plain":{"$ref":"#/$defs/F"}},
          "$defs":{
            "A":{"$ref":"#/$defs/B"},"B":{"$ref":"#/$defs/C"},
            "C":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
            "D":{"$ref":"#/$defs/E"},
            "E":{"$ref":"#/$defs/F","compliance":[{"metadataType":"PII","details":""}]},
            "F":{"type":"string"}
          }
        }
        """);

    void Because()
    {
        _terminal = _schema.Properties["terminal"];
        _intermediate = _schema.Properties["intermediate"];
    }

    [Fact] void should_detect_terminal_metadata() => _terminal.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_detect_intermediate_metadata() => _intermediate.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_match_terminal_metadata_types() => _terminal.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_match_intermediate_metadata_types() => _intermediate.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_match_a_different_metadata_type() => _intermediate.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "Other").ShouldBeFalse();
    [Fact] void should_protect_a_terminal_value_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_terminal).ShouldBeTrue();
    [Fact] void should_protect_an_intermediate_value_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_intermediate).ShouldBeTrue();
    [Fact] void should_leave_plain_values_unprotected() => ConverterSchemas.HasProtectedValueMetadata(_schema.Properties["plain"]).ShouldBeFalse();
}

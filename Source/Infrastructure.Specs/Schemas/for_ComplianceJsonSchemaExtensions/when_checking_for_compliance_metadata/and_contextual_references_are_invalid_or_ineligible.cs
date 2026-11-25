// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_contextual_references_are_invalid_or_ineligible : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _recursive;
    JsonSchemaProperty _invalid;
    JsonSchemaProperty _missing;
    JsonSchemaProperty _unsupported;
    JsonSchemaProperty _protectedNonRecursive;
    JsonSchemaProperty _protectedWithoutAnchor;
    JsonSchemaProperty _plainWithoutAnchor;
    JsonSchemaProperty _invalidAnchor;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$recursiveAnchor":false,
          "properties":{
            "recursive":{"$recursiveRef":"#"},
            "invalid":{"$dynamicRef":42},
            "missing":{"$dynamicRef":"#missing"},
            "unsupported":{"$recursiveRef":"#/$defs/plain"}
          },
          "$defs":{"plain":{"type":"string"}}
        }
        """);

    void Because()
    {
        _recursive = JsonSchema.FromJson("""{"$recursiveAnchor":false,"properties":{"value":{"$recursiveRef":"#"}}}""").Properties["value"];
        _invalid = _schema.Properties["invalid"];
        _missing = _schema.Properties["missing"];
        _unsupported = _schema.Properties["unsupported"];
        _protectedNonRecursive = JsonSchema.FromJson("""{"$recursiveAnchor":false,"compliance":[{"metadataType":"PII","details":""}],"properties":{"value":{"$recursiveRef":"#"}}}""").Properties["value"];
        _protectedWithoutAnchor = JsonSchema.FromJson("""{"compliance":[{"metadataType":"PII","details":""}],"properties":{"value":{"$recursiveRef":"#"}}}""").Properties["value"];
        _plainWithoutAnchor = JsonSchema.FromJson("""{"properties":{"value":{"$recursiveRef":"#"}}}""").Properties["value"];
        _invalidAnchor = JsonSchema.FromJson("""{"properties":{"value":{"$dynamicRef":"#/$defs/invalid"}},"$defs":{"invalid":{"$dynamicAnchor":42,"type":"string"}}}""").Properties["value"];
    }

    [Fact] void should_leave_a_proven_non_recursive_root_plain() => _recursive.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_fail_closed_for_an_invalid_value() => _invalid.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_fail_closed_for_a_missing_anchor() => _missing.HasSecurityMetadata().ShouldBeTrue();
    [Fact] void should_fail_closed_for_an_unsupported_recursive_shape() => ConverterSchemas.HasProtectedValueMetadata(_unsupported).ShouldBeTrue();
    [Fact] void should_treat_invalid_values_as_indeterminate_compliance() => _invalid.HasSchemaMetadata(SchemaMetadataCategory.Compliance, _ => true).ShouldBeTrue();
    [Fact] void should_not_invent_metadata_for_invalid_values() => _invalid.GetComplianceMetadata().ShouldBeEmpty();
    [Fact] void should_not_skip_static_metadata_when_the_root_is_not_recursive() => _protectedNonRecursive.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_skip_static_metadata_when_the_recursive_anchor_is_missing() => _protectedWithoutAnchor.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_leave_a_proven_plain_root_without_an_anchor_plain() => _plainWithoutAnchor.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_fail_closed_for_a_non_string_dynamic_anchor() => _invalidAnchor.HasComplianceMetadata().ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_dynamic_reference_reaches_a_protected_anchor : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _property;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$dynamicRef":"#personal"}},
          "$defs":{"personal":{"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}}
        }
        """);

    void Because() => _property = _schema.Properties["value"];

    [Fact] void should_classify_the_original_property_as_protected() => _property.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_find_the_concrete_metadata_type() => _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_preserve_protection_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_property).ShouldBeTrue();
}

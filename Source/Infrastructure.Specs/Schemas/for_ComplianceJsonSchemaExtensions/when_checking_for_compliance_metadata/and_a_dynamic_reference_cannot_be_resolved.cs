// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_dynamic_reference_cannot_be_resolved : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _property;

    void Establish() => _schema = JsonSchema.FromJson("""
        {"properties":{"value":{"$dynamicRef":"https://unavailable.example/schema#personal"}}}
        """);

    void Because() => _property = _schema.Properties["value"];

    [Fact] void should_not_classify_it_as_plain_compliance() => _property.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_not_classify_it_as_plain_security() => _property.HasSecurityMetadata().ShouldBeTrue();
    [Fact] void should_not_invent_a_compliance_handler() => _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeFalse();
    [Fact] void should_not_invent_a_security_handler() => _property.HasSchemaMetadata(SchemaMetadataCategory.Security, type => type == "Secret").ShouldBeFalse();
    [Fact] void should_preserve_protection_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_property).ShouldBeTrue();
    [Fact] void should_keep_an_unresolved_static_reference_plain() => JsonSchema.FromJson("""{"$ref":"https://unavailable.example/schema"}""").HasComplianceMetadata().ShouldBeFalse();
}

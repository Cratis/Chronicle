// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_contextual_target_has_an_unresolved_static_reference : Specification
{
    JsonSchemaProperty _contextual;
    JsonSchemaProperty _static;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
              "properties":{
                "contextual":{"$dynamicRef":"#/$defs/target"},
                "static":{"$ref":"#/$defs/target"}
              },
              "$defs":{"target":{"$ref":"https://unavailable.example/schema"}}
            }
            """);
        _contextual = schema.Properties["contextual"];
        _static = schema.Properties["static"];
    }

    [Fact] void should_treat_the_unresolved_contextual_path_as_protected() => _contextual.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_conservatively_classify_personal_data() => _contextual.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_not_invent_a_handler() => _contextual.GetComplianceMetadata().ShouldBeEmpty();
    [Fact] void should_not_invent_a_security_match() => _contextual.HasSchemaMetadata(SchemaMetadataCategory.Security, _ => true).ShouldBeFalse();
    [Fact] void should_preserve_protection_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_contextual).ShouldBeTrue();
    [Fact] void should_preserve_static_only_category_behavior() => _static.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_preserve_static_only_predicate_behavior() => _static.HasSchemaMetadata(SchemaMetadataCategory.Compliance, _ => true).ShouldBeFalse();
    [Fact] void should_preserve_static_only_conversion_behavior() => ConverterSchemas.HasProtectedValueMetadata(_static).ShouldBeFalse();
}

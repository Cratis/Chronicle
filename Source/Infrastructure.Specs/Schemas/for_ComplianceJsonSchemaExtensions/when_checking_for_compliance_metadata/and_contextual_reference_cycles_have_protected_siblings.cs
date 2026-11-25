// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_contextual_reference_cycles_have_protected_siblings : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _dynamic;
    JsonSchemaProperty _recursive;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"dynamic":{"$ref":"#/$defs/start"},"recursive":{"$ref":"#/$defs/recursive"}},
          "$defs":{
            "start":{"$dynamicRef":"#loop","allOf":[{"security":[{"metadataType":"Secret","details":""}]}]},
            "loop":{"$dynamicAnchor":"loop","$ref":"#/$defs/start","compliance":[{"metadataType":"PII","details":""}]},
            "recursive":{"$id":"recursive","$recursiveAnchor":true,"$recursiveRef":"#","properties":{"protected":{"compliance":[{"metadataType":"PII","details":""}]}}}
          }
        }
        """);

    void Because()
    {
        _dynamic = _schema.Properties["dynamic"];
        _recursive = _schema.Properties["recursive"];
    }

    [Fact] void should_find_metadata_next_to_a_mixed_cycle() => _dynamic.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_find_a_composition_sibling() => _dynamic.HasSecurityMetadata().ShouldBeTrue();
    [Fact] void should_find_a_child_next_to_a_recursive_cycle() => _recursive.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII").ShouldBeTrue();
    [Fact] void should_preserve_conversion_protection() => ConverterSchemas.HasProtectedValueMetadata(_dynamic).ShouldBeTrue();
}

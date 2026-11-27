// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_recursive_candidates_are_separate_from_instance_annotations : Specification
{
    JsonSchemaProperty _plain;
    JsonSchemaProperty _protected;
    bool _found;

    void Establish()
    {
        _plain = Create(includeCandidate: false);
        _protected = Create(includeCandidate: true);
    }

    void Because() => _found = _protected.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII");

    [Fact] void should_find_recursive_candidates_outside_the_entered_route() => _found.ShouldBeTrue();
    [Fact] void should_preserve_conversion_protection() => ConverterSchemas.HasProtectedValueMetadata(_protected).ShouldBeTrue();
    [Fact] void should_not_index_instance_annotations() => _plain.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_prove_the_plain_cycle_complete() => _plain.MetadataReferences.Indeterminate.ShouldBeFalse();
    [Fact] void should_expand_the_recursive_group_only_once() => _protected.MetadataReferences.AnchorExpansions.ShouldEqual(1);
    [Fact] void should_not_invent_own_metadata() => _protected.GetComplianceMetadata().ShouldBeEmpty();

    static JsonSchemaProperty Create(bool includeCandidate)
    {
        var schema = JsonSchema.FromJson("""
            {"$id":"https://schemas.example/document","$recursiveAnchor":true,"properties":{"value":{"$recursiveRef":"#"}}}
            """);
        var candidate = JsonNode.Parse("""
            {"$id":"candidate","$recursiveAnchor":true,"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}
            """)!;
        schema.Node["default"] = candidate.DeepClone();
        schema.Node["const"] = candidate.DeepClone();
        schema.Node["examples"] = new JsonArray(candidate.DeepClone());
        schema.Node["enum"] = new JsonArray(candidate.DeepClone());
        if (includeCandidate) schema.Node["dependentSchemas"] = new JsonObject { ["candidate"] = candidate };
        return schema.Properties["value"];
    }
}

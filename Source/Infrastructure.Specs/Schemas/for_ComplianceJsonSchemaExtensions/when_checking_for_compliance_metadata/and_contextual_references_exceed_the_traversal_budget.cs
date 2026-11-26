// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_contextual_references_exceed_the_traversal_budget : Specification
{
    JsonSchema _schema;
    JsonSchemaProperty _property;

    void Establish()
    {
        var definitions = new JsonObject();
        for (var index = 0; index < 4200; index++)
        {
            definitions[$"node{index}"] = new JsonObject { ["$dynamicRef"] = $"#/$defs/node{index + 1}" };
        }
        definitions["node4200"] = new JsonObject { ["type"] = "string" };
        _schema = new JsonSchema(new JsonObject
        {
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$dynamicRef"] = "#/$defs/node0" } },
            ["$defs"] = definitions
        });
    }

    void Because() => _property = _schema.Properties["value"];

    [Fact] void should_not_claim_the_unexplored_graph_is_plain() => _property.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_preserve_protection_during_conversion() => ConverterSchemas.HasProtectedValueMetadata(_property).ShouldBeTrue();
    [Fact] void should_treat_unexplored_compliance_as_protected() => _property.HasSchemaMetadata(SchemaMetadataCategory.Compliance, _ => true).ShouldBeTrue();
    [Fact] void should_not_manufacture_a_handler_type() => _property.GetComplianceMetadata().ShouldBeEmpty();
    [Fact] void should_record_budget_exhaustion() => _property.MetadataReferences.Indeterminate.ShouldBeTrue();
    [Fact] void should_bound_the_number_of_unique_nodes() => _property.MetadataReferences.UniqueNodes.ShouldEqual(4096);
}

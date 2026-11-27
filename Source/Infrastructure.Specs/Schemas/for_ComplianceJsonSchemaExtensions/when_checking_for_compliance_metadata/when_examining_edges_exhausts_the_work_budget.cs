// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class when_examining_edges_exhausts_the_work_budget : Specification
{
    [Theory]
    [InlineData(4200)]
    [InlineData(10000)]
    void should_charge_boolean_schema_edges_before_filtering(int count)
    {
        var properties = new JsonObject();
        for (var index = 0; index < count; index++) properties[$"plain{index}"] = true;
        var schema = new JsonSchema(new JsonObject { ["properties"] = properties });
        var reach = schema.MetadataGraph;
        reach.ExaminedEdges.ShouldEqual(4096);
        reach.Indeterminate.ShouldBeTrue();
        reach.UniqueNodes.ShouldEqual(0);
        schema.HasComplianceMetadata().ShouldBeTrue();
        schema.GetComplianceMetadata().ShouldBeEmpty();
    }

    [Fact]
    void should_charge_duplicate_edges_before_deduplication()
    {
        var alternatives = new JsonArray();
        for (var index = 0; index < 1200; index++) alternatives.Add(new JsonObject { ["$ref"] = "#/$defs/plain" });
        var schema = new JsonSchema(new JsonObject
        {
            ["allOf"] = alternatives,
            ["$defs"] = JsonNode.Parse("""{"plain":{"type":"string"}}""")
        });
        _ = schema.AllOf;
        var reach = schema.MetadataGraph;
        reach.ExaminedEdges.ShouldEqual(4096);
        reach.UniqueNodes.ShouldBeLessThan(4096);
        reach.Indeterminate.ShouldBeTrue();
    }
}

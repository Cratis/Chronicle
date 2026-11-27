// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_embedded_resources_reference_each_other : Specification
{
    JsonSchema _plain;
    JsonSchema _protected;
    bool _plainResult;
    bool _protectedResult;
    TimeSpan _elapsed;

    void Establish()
    {
        _plain = ResourceGraph(false);
        _protected = ResourceGraph(true);
    }

    void Because()
    {
        var timer = Stopwatch.StartNew();
        _plainResult = _plain.HasComplianceMetadata();
        _protectedResult = _protected.HasComplianceMetadata();
        _elapsed = timer.Elapsed;
    }

    [Fact] void should_not_classify_the_plain_graph_as_protected() => _plainResult.ShouldBeFalse();
    [Fact] void should_reach_the_protected_resource() => _protectedResult.ShouldBeTrue();
    [Fact] void should_complete_both_graphs_in_less_than_one_second() => _elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    [Fact] void should_finish_the_plain_graph_without_uncertainty() => _plain.MetadataGraph.Indeterminate.ShouldBeFalse();
    [Fact] void should_finish_the_protected_graph_without_uncertainty() => _protected.MetadataGraph.Indeterminate.ShouldBeFalse();

    static JsonSchema ResourceGraph(bool protect)
    {
        var definitions = new JsonObject();
        for (var resource = 0; resource < 10; resource++)
        {
            var properties = new JsonObject();
            for (var target = 0; target < 10; target++)
            {
                properties[$"node{target}"] = new JsonObject { ["$ref"] = $"https://schemas.example/node{target}" };
            }

            definitions[$"node{resource}"] = new JsonObject
            {
                ["$id"] = $"https://schemas.example/node{resource}",
                ["type"] = "object",
                ["properties"] = properties
            };
        }

        if (protect)
        {
            definitions["node9"]!["properties"]!["secret"] = JsonNode.Parse("""{"type":"string","compliance":[{"metadataType":"PII","details":""}]}""");
        }

        return new JsonSchema(new JsonObject
        {
            ["$defs"] = definitions,
            ["properties"] = new JsonObject { ["value"] = new JsonObject { ["$ref"] = "https://schemas.example/node0" } }
        });
    }
}

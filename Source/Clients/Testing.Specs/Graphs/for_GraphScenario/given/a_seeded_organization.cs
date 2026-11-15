// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.given;

public class a_seeded_organization : Specification
{
    internal static readonly JsonObject Vectors = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Graphs", "for_GraphScenario", "Fixtures", "graph-views-query-vectors.json")))!.AsObject();

    internal GraphScenario _scenario;

    internal static object? ValueOf(JsonNode? node) => node?.GetValueKind() switch
    {
        null or JsonValueKind.Null => null,
        JsonValueKind.String => node.GetValue<string>(),
        JsonValueKind.Number => node.GetValue<decimal>(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => node.ToJsonString()
    };

    internal static Dictionary<string, object?> PropertiesOf(JsonNode? node) =>
        node?.AsObject().ToDictionary(_ => _.Key, _ => ValueOf(_.Value)) ?? [];

    async Task Establish()
    {
        _scenario = new GraphScenario();
        foreach (var node in Vectors["nodes"]!.AsArray())
        {
            await _scenario.SeedNode(node!["label"]!.GetValue<string>(), node["key"]!.GetValue<string>(), PropertiesOf(node["properties"]));
        }

        foreach (var edge in Vectors["edges"]!.AsArray())
        {
            await _scenario.SeedEdge(
                edge!["type"]!.GetValue<string>(),
                edge["source"]!["key"]!.GetValue<string>(),
                edge["target"]!["key"]!.GetValue<string>(),
                ValueOf(edge["identity"]),
                PropertiesOf(edge["properties"]));
        }
    }

    void Destroy() => _scenario.Dispose();
}

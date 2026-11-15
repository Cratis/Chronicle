// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.given;

/// <summary>
/// The Organization graph and its views as the shared definition of graph views declares them.
/// </summary>
public class an_organization_definition : Specification
{
    internal static readonly JsonObject Vectors = Load("graph-views-query-vectors.json");
    internal static readonly JsonObject Definition = Load("graph-views-organization.json");

    protected GraphDefinition _graph;
    protected IReadOnlyList<GraphView> _views;

    internal static object? ValueOf(JsonNode? node) => node?.GetValueKind() switch
    {
        null or JsonValueKind.Null => null,
        JsonValueKind.String => node.GetValue<string>(),
        JsonValueKind.Number => node.GetValue<decimal>(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => node.ToJsonString()
    };

    protected static GraphDefinition OrganizationGraph()
    {
        var schema = Definition["schema"]!;
        return new(
            Definition["graph"]!.GetValue<string>(),
            [.. schema["nodes"]!.AsArray().Select(node => new GraphNodeDefinition(
                node!["label"]!.GetValue<string>(),
                node["key"]!.GetValue<string>(),
                SchemaOf(node["properties"]!.AsArray())))],
            [.. schema["edges"]!.AsArray().Select(edge => new GraphEdgeDefinition(
                edge!["type"]!.GetValue<string>(),
                edge["source"]!.GetValue<string>(),
                edge["target"]!.GetValue<string>(),
                edge["identifiedBy"]?.GetValue<string>(),
                SchemaOf(edge["properties"]!.AsArray())))]);
    }

    protected static IReadOnlyList<GraphView> OrganizationViews() =>
        [.. Definition["views"]!.AsArray().Select(view => new GraphView(
            view!["name"]!.GetValue<string>(),
            view["root"]!.GetValue<string>(),
            [.. view["members"]!.AsArray().Select(member => new GraphViewMember(
                member!["name"]!.GetValue<string>(),
                Enum.Parse<GraphViewMemberKind>(member["kind"]!.GetValue<string>(), true),
                member["source"]?.GetValue<string>(),
                member["edge"]?.GetValue<string>(),
                member["direction"] is null ? null : Enum.Parse<GraphViewDirection>(member["direction"]!.GetValue<string>(), true),
                member["cardinality"] is null ? null : Enum.Parse<GraphViewCardinality>(member["cardinality"]!.GetValue<string>(), true),
                member["view"]?.GetValue<string>()))]))];

    protected static Dictionary<string, object?> PropertiesOf(JsonNode? node) =>
        node?.AsObject().ToDictionary(_ => _.Key, _ => ValueOf(_.Value)) ?? [];

    void Establish()
    {
        _graph = OrganizationGraph();
        _views = OrganizationViews();
    }

    static JsonObject Load(string name) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Projections", "Engine", "Graphs", "Views", "for_GraphViewEvaluator", "Fixtures", name)))!.AsObject();

    static JsonSchema SchemaOf(JsonArray properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var property in properties) schema.Properties[property!.GetValue<string>()] = new JsonSchemaProperty { Type = JsonObjectType.String };
        return schema;
    }
}

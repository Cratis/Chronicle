// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Graphs;

/// <summary>
/// Represents the definition of a property graph: its node labels and edge types.
/// </summary>
/// <param name="Name">The name of the graph.</param>
/// <param name="Nodes">The <see cref="GraphNodeDefinition">node definitions</see>.</param>
/// <param name="Edges">The <see cref="GraphEdgeDefinition">edge definitions</see>.</param>
/// <remarks>
/// A graph is registered like a read model. It travels as the read model schema: the root schema is an object whose
/// properties are the node labels and edge types, each carrying a few "x-graph-*" annotations. This keeps the
/// registration wire format and the read model storage unchanged. Use <see cref="ToSchema"/> and <see cref="FromSchema"/>
/// to move between the two forms.
/// </remarks>
public record GraphDefinition(string Name, IReadOnlyList<GraphNodeDefinition> Nodes, IReadOnlyList<GraphEdgeDefinition> Edges)
{
    const string GraphMarker = "x-graph";
    const string KindKey = "x-graph-kind";
    const string KeyKey = "x-graph-key";
    const string SourceKey = "x-graph-source";
    const string TargetKey = "x-graph-target";
    const string IdentityKey = "x-graph-identity";

    /// <summary>
    /// Gets whether a schema describes a graph.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> to check.</param>
    /// <returns>True if the schema was produced from a <see cref="GraphDefinition"/>, false if not.</returns>
    public static bool IsGraphSchema(JsonSchema schema) => Parse(schema)[GraphMarker]?.GetValue<bool>() == true;

    /// <summary>
    /// Create a <see cref="GraphDefinition"/> from the schema form produced by <see cref="ToSchema"/>.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> to read.</param>
    /// <returns>The <see cref="GraphDefinition"/>.</returns>
    /// <exception cref="InvalidGraphSchema">Thrown when the schema does not describe a graph.</exception>
    public static GraphDefinition FromSchema(JsonSchema schema)
    {
        if (!IsGraphSchema(schema)) throw new InvalidGraphSchema();

        var nodes = new List<GraphNodeDefinition>();
        var edges = new List<GraphEdgeDefinition>();
        var root = Parse(schema);
        foreach (var (name, property) in root["properties"]!.AsObject())
        {
            var node = property!.AsObject();
            var kind = node[KindKey]?.GetValue<string>();
            var element = new JsonSchema((JsonObject)JsonNode.Parse(node.ToJsonString())!);
            if (kind == nameof(GraphElementKind.Node))
            {
                nodes.Add(new(name, node[KeyKey]!.GetValue<string>(), element));
            }
            else if (kind == nameof(GraphElementKind.Edge))
            {
                edges.Add(new(
                    name,
                    node[SourceKey]!.GetValue<string>(),
                    node[TargetKey]!.GetValue<string>(),
                    node[IdentityKey]?.GetValue<string>(),
                    element));
            }
        }

        return new(root["title"]?.GetValue<string>() ?? string.Empty, nodes, edges);
    }

    /// <summary>
    /// Converts the definition to the schema form that is registered as the read model schema.
    /// </summary>
    /// <returns>The <see cref="JsonSchema"/>.</returns>
    public JsonSchema ToSchema()
    {
        var properties = new JsonObject();
        foreach (var node in Nodes)
        {
            var element = CopyOf(node.Schema);
            element[KindKey] = nameof(GraphElementKind.Node);
            element[KeyKey] = node.KeyProperty;
            properties[node.Label] = element;
        }

        foreach (var edge in Edges)
        {
            var element = CopyOf(edge.Schema);
            element[KindKey] = nameof(GraphElementKind.Edge);
            element[SourceKey] = edge.SourceLabel;
            element[TargetKey] = edge.TargetLabel;
            if (edge.IdentifyingProperty is not null) element[IdentityKey] = edge.IdentifyingProperty;
            properties[edge.Type] = element;
        }

        return new JsonSchema(new JsonObject
        {
            ["type"] = "object",
            ["title"] = Name,
            [GraphMarker] = true,
            ["properties"] = properties
        });
    }

    /// <summary>
    /// Gets the node definition for a label.
    /// </summary>
    /// <param name="label">The node label.</param>
    /// <returns>The <see cref="GraphNodeDefinition"/>.</returns>
    /// <exception cref="UnknownGraphElement">Thrown when the graph has no such node label.</exception>
    public GraphNodeDefinition GetNode(string label) =>
        Nodes.FirstOrDefault(_ => _.Label == label) ?? throw new UnknownGraphElement(Name, GraphElementKind.Node, label);

    /// <summary>
    /// Gets the edge definition for a type.
    /// </summary>
    /// <param name="type">The edge type.</param>
    /// <returns>The <see cref="GraphEdgeDefinition"/>.</returns>
    /// <exception cref="UnknownGraphElement">Thrown when the graph has no such edge type.</exception>
    public GraphEdgeDefinition GetEdge(string type) =>
        Edges.FirstOrDefault(_ => _.Type == type) ?? throw new UnknownGraphElement(Name, GraphElementKind.Edge, type);

    static JsonObject CopyOf(JsonSchema schema) => Parse(schema);

    static JsonObject Parse(JsonSchema schema) => (JsonObject)JsonNode.Parse(schema.ToJson())!;
}

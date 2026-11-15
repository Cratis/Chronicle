// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Writes the schema form of a graph: an object schema whose properties are the node labels and edge types.
/// </summary>
/// <remarks>
/// This is the form the kernel reads a graph from. The annotations are the ones of the kernel's graph definition.
/// </remarks>
/// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for the schema of the node and edge types.</param>
internal class GraphSchemaWriter(IJsonSchemaGenerator schemaGenerator)
{
    /// <summary>
    /// Write the schema of a graph.
    /// </summary>
    /// <param name="model">The <see cref="GraphModel"/>.</param>
    /// <returns>The schema as JSON.</returns>
    /// <exception cref="NestedObjectInGraph">Thrown when a node or an edge has an object property.</exception>
    public string Write(GraphModel model)
    {
        var properties = new JsonObject();
        foreach (var node in model.Nodes)
        {
            var element = Element(node.Type, []);
            element["x-graph-kind"] = "Node";
            element["x-graph-key"] = node.KeyProperty;
            properties[node.Label] = element;
        }

        foreach (var edge in model.Edges)
        {
            var element = Element(edge.Type, edge.ExcludedProperties);
            element["x-graph-kind"] = "Edge";
            element["x-graph-source"] = edge.SourceNode.Name;
            element["x-graph-target"] = edge.TargetNode.Name;
            if (edge.IdentifyingProperty is not null) element["x-graph-identity"] = edge.IdentifyingProperty;
            properties[edge.EdgeType] = element;
        }

        return new JsonObject { ["type"] = "object", ["title"] = model.Name, ["x-graph"] = true, ["properties"] = properties }.ToJsonString();
    }

    /// <summary>
    /// Get the names of the properties of a node or edge type, as they are named in the graph.
    /// </summary>
    /// <param name="type">The node or edge type.</param>
    /// <param name="excluded">Names of members that are not properties.</param>
    /// <returns>The property names with their schemas.</returns>
    /// <exception cref="NestedObjectInGraph">Thrown when a property is an object.</exception>
    public IReadOnlyDictionary<string, JsonSchemaProperty> PropertiesOf(Type type, IEnumerable<string> excluded)
    {
        var schema = schemaGenerator.GenerateForReadModel(type);
        var skip = excluded.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, JsonSchemaProperty>();
        foreach (var (name, property) in schema.Properties)
        {
            if (skip.Contains(name)) continue;
            if (IsObject(property) || (property.Item is { } item && IsObject(item)))
            {
                throw new NestedObjectInGraph(type, name);
            }

            result[name] = property;
        }

        return result;
    }

    static bool IsObject(JsonSchema schema) => schema.ActualSchema.Type.HasFlag(JsonObjectType.Object);

    JsonObject Element(Type type, IEnumerable<string> excluded)
    {
        var properties = new JsonObject();
        foreach (var (name, property) in PropertiesOf(type, excluded))
        {
            properties[name] = JsonNode.Parse(property.ActualSchema.ToJson());
        }

        return new() { ["type"] = "object", ["properties"] = properties };
    }
}

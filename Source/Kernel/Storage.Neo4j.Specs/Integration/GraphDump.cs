// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// Describes a graph as text, so two sinks can be compared on the graph they hold.
/// </summary>
/// <remarks>
/// A node is identified in Neo4j by its key property, and an edge with an identity by its identifying property, so those
/// properties are always stored. The description leaves them out for both sinks, which lets a scenario give them or not.
/// </remarks>
public static class GraphDump
{
    static readonly string[] _nodeLabels = ["Group", "Person"];
    static readonly string[] _edgeTypes = ["MEMBER_OF", "VISITED"];

    /// <summary>
    /// Describe everything in a graph.
    /// </summary>
    /// <param name="sink">The <see cref="IGraphSink"/>.</param>
    /// <returns>The description, one line per node and edge.</returns>
    public static async Task<IReadOnlyList<string>> Describe(IGraphSink sink)
    {
        var lines = new List<string>();
        foreach (var label in _nodeLabels)
        {
            var nodes = await sink.GetNodes(label, 0, 1000);
            lines.Add($"{label}: {nodes.TotalCount}");
            lines.AddRange(nodes.Elements.Select(_ => Describe(_)));
        }

        foreach (var type in _edgeTypes)
        {
            var edges = await sink.GetEdges(type, 0, 1000);
            lines.Add($"{type}: {edges.TotalCount}");
            lines.AddRange(edges.Elements.Select(_ => Describe(_)));
        }

        return lines;
    }

    /// <summary>
    /// Describe a node.
    /// </summary>
    /// <param name="node">The <see cref="GraphNode"/>, or null.</param>
    /// <returns>The description.</returns>
    public static string Describe(GraphNode? node) => node is null
        ? "absent"
        : $"node {node.Reference.Label}/{GraphValues.TextOf(node.Reference.Key)} watermark={Watermark(node.Watermark)} {Properties(node.Properties, KeyOf(node.Reference.Label))}";

    /// <summary>
    /// Describe an edge.
    /// </summary>
    /// <param name="edge">The <see cref="GraphEdge"/>, or null.</param>
    /// <returns>The description.</returns>
    public static string Describe(GraphEdge? edge) => edge is null
        ? "absent"
        : $"edge {edge.Reference.Type}/{GraphValues.TextOf(edge.Reference.Source.Key)}->{GraphValues.TextOf(edge.Reference.Target.Key)}/{GraphValues.TextOf(edge.Reference.Identity)} watermark={Watermark(edge.Watermark)} {Properties(edge.Properties, edge.Reference.Type == "VISITED" ? "visitId" : null)}";

    static string KeyOf(string label) => label == "Person" ? "personId" : "groupId";

    static string Watermark(Concepts.Events.EventSequenceNumber watermark) => watermark.IsUnavailable ? "none" : watermark.Value.ToString(CultureInfo.InvariantCulture);

    static string Properties(IReadOnlyDictionary<string, object?> properties, string? left) =>
        "{" + string.Join(", ", properties.Where(_ => _.Key != left).OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}={Format(_.Value)}")) + "}";

    static string Format(object? value) => value switch
    {
        null => "null",
        string text => $"'{text}'",
        IEnumerable list => "[" + string.Join(", ", list.Cast<object?>().Select(Format)) + "]",
        _ => $"{value.GetType().Name}:{Convert.ToString(value, CultureInfo.InvariantCulture)}"
    };
}

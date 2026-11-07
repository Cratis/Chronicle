// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

#pragma warning disable SA1402, SA1649 // The records of a graph read live together.
namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Represents a node as stored in Neo4j.
/// </summary>
/// <param name="Label">The label of the node.</param>
/// <param name="Properties">The properties of the node, without the ones the sink keeps for itself.</param>
/// <param name="Internal">The properties the sink keeps for itself, such as the watermark and the namespace.</param>
public record GraphNode(string Label, IReadOnlyDictionary<string, object?> Properties, IReadOnlyDictionary<string, object?> Internal)
{
    /// <summary>
    /// Gets the sequence number of the last event that wrote the node, or null when only an edge has created it.
    /// </summary>
    public long? Watermark => Internal.TryGetValue("_watermark", out var value) ? value as long? : null;

    /// <summary>
    /// Gets the namespace the sink stamped on the node, which it does in single database mode only.
    /// </summary>
    public string? Namespace => Internal.TryGetValue("_namespace", out var value) ? value as string : null;

    /// <summary>
    /// Gets the owner the sink stamped on the node.
    /// </summary>
    public string? Owner => Internal.TryGetValue("_owner", out var value) ? value as string : null;

    /// <summary>
    /// Gets a property of the node.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>The value, or null when the node does not have it.</returns>
    public object? this[string name] => Properties.TryGetValue(name, out var value) ? value : null;
}

/// <summary>
/// Represents an edge as stored in Neo4j.
/// </summary>
/// <param name="Type">The type of the edge.</param>
/// <param name="Source">The node the edge starts at.</param>
/// <param name="Target">The node the edge ends at.</param>
/// <param name="Properties">The properties of the edge, without the ones the sink keeps for itself.</param>
/// <param name="Internal">The properties the sink keeps for itself.</param>
public record GraphEdge(string Type, GraphNode Source, GraphNode Target, IReadOnlyDictionary<string, object?> Properties, IReadOnlyDictionary<string, object?> Internal)
{
    /// <summary>
    /// Gets the sequence number of the last event that wrote the edge.
    /// </summary>
    public long? Watermark => Internal.TryGetValue("_watermark", out var value) ? value as long? : null;

    /// <summary>
    /// Gets a property of the edge.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>The value, or null when the edge does not have it.</returns>
    public object? this[string name] => Properties.TryGetValue(name, out var value) ? value : null;
}

/// <summary>
/// Represents everything in the graph at a point in time, read from Neo4j with Cypher of its own and not through the sink under test.
/// </summary>
/// <param name="Nodes">All the nodes.</param>
/// <param name="Edges">All the edges.</param>
public record GraphSnapshot(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges)
{
    /// <summary>
    /// Read the graph.
    /// </summary>
    /// <param name="driver">The <see cref="IDriver"/> to read with.</param>
    /// <param name="database">The database to read.</param>
    /// <returns>The <see cref="GraphSnapshot"/>.</returns>
    public static async Task<GraphSnapshot> Read(IDriver driver, string database)
    {
        await using var session = driver.AsyncSession(_ => _.WithDatabase(database));
        var nodes = await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync("MATCH (n) WHERE NOT '_Owner' IN labels(n) RETURN labels(n) AS labels, properties(n) AS properties");
            return await cursor.ToListAsync(_ => ToNode(_["labels"].As<IList<string>>()[0], _["properties"].As<IReadOnlyDictionary<string, object>>()));
        });

        var edges = await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(
                "MATCH (s)-[r]->(t) WHERE NOT '_Owner' IN labels(s) RETURN type(r) AS type, labels(s) AS sourceLabels, properties(s) AS source, labels(t) AS targetLabels, properties(t) AS target, properties(r) AS properties");
            return await cursor.ToListAsync(_ => ToEdge(
                _["type"].As<string>(),
                ToNode(_["sourceLabels"].As<IList<string>>()[0], _["source"].As<IReadOnlyDictionary<string, object>>()),
                ToNode(_["targetLabels"].As<IList<string>>()[0], _["target"].As<IReadOnlyDictionary<string, object>>()),
                _["properties"].As<IReadOnlyDictionary<string, object>>()));
        });

        return new(nodes, edges);
    }

    /// <summary>
    /// Get the nodes with a label.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <returns>The matching nodes.</returns>
    public IEnumerable<GraphNode> NodesOf(string label) => Nodes.Where(_ => _.Label == label);

    /// <summary>
    /// Get the edges of a type.
    /// </summary>
    /// <param name="type">The type of the edge.</param>
    /// <returns>The matching edges.</returns>
    public IEnumerable<GraphEdge> EdgesOf(string type) => Edges.Where(_ => _.Type == type);

    /// <summary>
    /// Find a node by label and key.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="keyProperty">The name of the key property.</param>
    /// <param name="key">The key, compared as text.</param>
    /// <returns>The node, or null when there is none.</returns>
    public GraphNode? Node(string label, string keyProperty, object key) =>
        NodesOf(label).SingleOrDefault(_ => Equals(_[keyProperty]?.ToString(), key.ToString()));

    /// <summary>
    /// Find the edges of a type between two nodes, identified by their keys.
    /// </summary>
    /// <param name="type">The type of the edge.</param>
    /// <param name="sourceKeyProperty">The name of the key property of the source node.</param>
    /// <param name="source">The key of the source node.</param>
    /// <param name="targetKeyProperty">The name of the key property of the target node.</param>
    /// <param name="target">The key of the target node.</param>
    /// <returns>The matching edges.</returns>
    public IEnumerable<GraphEdge> EdgesBetween(string type, string sourceKeyProperty, object source, string targetKeyProperty, object target) =>
        EdgesOf(type).Where(_ => Equals(_.Source[sourceKeyProperty]?.ToString(), source.ToString()) && Equals(_.Target[targetKeyProperty]?.ToString(), target.ToString()));

    /// <summary>
    /// Describe the graph in a stable, comparable form: every node and edge on a line of its own with its properties sorted by name.
    /// </summary>
    /// <returns>The lines, sorted.</returns>
    public IReadOnlyList<string> Describe() =>
        [.. Nodes.Select(_ => $"node {_.Label} {Properties(_.Properties)}")
            .Concat(Edges.Select(_ => $"edge {_.Type} ({_.Source.Label} {Properties(_.Source.Properties)}) -> ({_.Target.Label} {Properties(_.Target.Properties)}) {Properties(_.Properties)}"))
            .Order(StringComparer.Ordinal)];

    static string Properties(IReadOnlyDictionary<string, object?> properties) =>
        "{" + string.Join(", ", properties.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}: {Format(_.Value)}")) + "}";

    static string Format(object? value) => value switch
    {
        null => "null",
        System.Collections.IList list => "[" + string.Join(", ", list.Cast<object?>().Select(Format)) + "]",
        DateTimeOffset offset => offset.UtcDateTime.ToString("O"),
        var other => Convert.ToString(other, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
    };

    static GraphNode ToNode(string label, IReadOnlyDictionary<string, object> properties) =>
        new(label, Visible(properties), Internal(properties));

    static GraphEdge ToEdge(string type, GraphNode source, GraphNode target, IReadOnlyDictionary<string, object> properties) =>
        new(type, source, target, Visible(properties), Internal(properties));

    static Dictionary<string, object?> Visible(IReadOnlyDictionary<string, object> properties) =>
        properties.Where(_ => !_.Key.StartsWith('_')).ToDictionary(_ => _.Key, _ => FromDriver(_.Value));

    static Dictionary<string, object?> Internal(IReadOnlyDictionary<string, object> properties) =>
        properties.Where(_ => _.Key.StartsWith('_')).ToDictionary(_ => _.Key, _ => FromDriver(_.Value));

    static object? FromDriver(object? value) => value switch
    {
        ZonedDateTime zoned => zoned.ToDateTimeOffset(),
        IList<object?> list => list.Select(FromDriver).ToList(),
        _ => value
    };
}

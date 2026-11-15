// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Holds the nodes and edges a query reaches, and reads rows from them.
/// </summary>
internal sealed class GraphViewSnapshot
{
    readonly GraphDefinition _graph;
    readonly Dictionary<string, GraphView> _views;
    readonly Dictionary<string, Dictionary<string, GraphNode>> _nodes;
    readonly Dictionary<string, IReadOnlyList<GraphEdge>> _edges;

    GraphViewSnapshot(
        GraphDefinition graph,
        Dictionary<string, GraphView> views,
        Dictionary<string, Dictionary<string, GraphNode>> nodes,
        Dictionary<string, IReadOnlyList<GraphEdge>> edges)
    {
        _graph = graph;
        _views = views;
        _nodes = nodes;
        _edges = edges;
    }

    /// <summary>
    /// Load the nodes and edges a view needs, through every relation.
    /// </summary>
    /// <param name="sink">The <see cref="IGraphSink"/> to read.</param>
    /// <param name="graph">The <see cref="GraphDefinition"/> of the graph.</param>
    /// <param name="views">The views by name.</param>
    /// <param name="root">The root <see cref="GraphView"/> of the query.</param>
    /// <returns>The <see cref="GraphViewSnapshot"/>.</returns>
    internal static async Task<GraphViewSnapshot> Load(IGraphSink sink, GraphDefinition graph, Dictionary<string, GraphView> views, GraphView root)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        var edgeTypes = new HashSet<string>(StringComparer.Ordinal);
        Collect(root, views, new HashSet<string>(StringComparer.Ordinal), labels, edgeTypes);

        var nodes = new Dictionary<string, Dictionary<string, GraphNode>>(StringComparer.Ordinal);
        foreach (var label in labels)
        {
            var page = await sink.GetNodes(label, 0, int.MaxValue);
            nodes[label] = page.Elements.ToDictionary(_ => GraphValues.TextOf(_.Reference.Key), StringComparer.Ordinal);
        }

        var edges = new Dictionary<string, IReadOnlyList<GraphEdge>>(StringComparer.Ordinal);
        foreach (var type in edgeTypes)
        {
            edges[type] = (await sink.GetEdges(type, 0, int.MaxValue)).Elements;
        }

        return new(graph, views, nodes, edges);
    }

    /// <summary>
    /// Get the roots of a view, distinct by key and ordered by key.
    /// </summary>
    /// <param name="view">The <see cref="GraphView"/>.</param>
    /// <returns>The nodes.</returns>
    internal IEnumerable<GraphNode> NodesOf(GraphView view) =>
        _nodes[view.Root].Values.OrderBy(_ => KeyOf(view, _), GraphViewKeys.Instance);

    /// <summary>
    /// Check whether a node satisfies a clause.
    /// </summary>
    /// <param name="view">The <see cref="GraphView"/> the clause is on.</param>
    /// <param name="element">The <see cref="GraphNode"/>.</param>
    /// <param name="clause">The <see cref="GraphViewClause"/>.</param>
    /// <returns>True if it does, false if not.</returns>
    internal bool Satisfies(GraphView view, GraphNode element, GraphViewClause clause) => clause switch
    {
        GraphViewPropertyClause property => GraphViewKeys.AreEqual(PropertyOf(view, element, SourceOf(view, property.Member)), property.Value),
        GraphViewRelatedClause related => Related(view, element, RelationOf(view, related.Member)).Any(item =>
            related.Clauses.All(nested => Satisfies(item.Target, item.Far, nested))),
        _ => false
    };

    /// <summary>
    /// Build the row of a node.
    /// </summary>
    /// <param name="view">The <see cref="GraphView"/>.</param>
    /// <param name="element">The <see cref="GraphNode"/>.</param>
    /// <param name="edge">The <see cref="GraphEdge"/> that was followed to reach it, or null for a root.</param>
    /// <returns>The <see cref="GraphViewRow"/>.</returns>
    internal GraphViewRow RowOf(GraphView view, GraphNode element, GraphEdge? edge)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var member in view.Members)
        {
            values[member.Name] = member.Kind switch
            {
                GraphViewMemberKind.Property => PropertyOf(view, element, member.Source!),
                GraphViewMemberKind.EdgeProperty => edge is null ? null : ValueOf(edge.Properties, member.Source!),
                _ => RelationValue(view, element, member)
            };
        }

        return new(values);
    }

    static void Collect(GraphView view, Dictionary<string, GraphView> views, HashSet<string> visited, HashSet<string> labels, HashSet<string> edgeTypes)
    {
        labels.Add(view.Root);
        if (!visited.Add(view.Name)) return;

        foreach (var member in view.Members.Where(_ => _.Kind == GraphViewMemberKind.Relation))
        {
            edgeTypes.Add(member.Edge!);
            Collect(views[member.View!], views, visited, labels, edgeTypes);
        }
    }

    static object? ValueOf(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value)
            ? value
            : properties.FirstOrDefault(_ => string.Equals(_.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    static GraphViewMember RelationOf(GraphView view, string member) =>
        view.Members.First(_ => _.Name == member);

    static string SourceOf(GraphView view, string member) =>
        view.Members.First(_ => _.Name == member).Source!;

    object? KeyOf(GraphView view, GraphNode element) =>
        PropertyOf(view, element, _graph.GetNode(view.Root).KeyProperty);

    object? PropertyOf(GraphView view, GraphNode element, string property)
    {
        var value = ValueOf(element.Properties, property);
        if (value is not null) return value;

        // The key of a node is always known, also for a bare node an edge created without properties.
        var key = _graph.GetNode(view.Root).KeyProperty;
        return string.Equals(property, key, StringComparison.OrdinalIgnoreCase) ? element.Reference.Key : null;
    }

    object? RelationValue(GraphView view, GraphNode element, GraphViewMember relation)
    {
        var items = Related(view, element, relation).Select(_ => RowOf(_.Target, _.Far, _.Edge)).ToList();
        return relation.Cardinality == GraphViewCardinality.One ? items.FirstOrDefault() : items;
    }

    IEnumerable<(GraphEdge Edge, GraphNode Far, GraphView Target)> Related(GraphView view, GraphNode element, GraphViewMember relation)
    {
        var definition = _graph.GetEdge(relation.Edge!);
        var direction = GraphViewResolution.ResolveDirection(definition, view.Root, relation.Direction, out _)!.Value;
        var outgoing = direction == GraphViewDirection.Outgoing;
        var target = _views[relation.View!];
        var key = GraphValues.TextOf(element.Reference.Key);

        return _edges[definition.Type]
            .Where(edge => (outgoing ? edge.Reference.Source : edge.Reference.Target) is var near && near.Label == view.Root && GraphValues.TextOf(near.Key) == key)
            .Select(edge => (Edge: edge, Far: FarNode(outgoing ? edge.Reference.Target : edge.Reference.Source)))
            .OrderBy(_ => KeyOf(target, _.Far), GraphViewKeys.Instance)
            .ThenBy(_ => _.Edge.Reference.Identity, GraphViewKeys.Instance)
            .Select(_ => (_.Edge, _.Far, target));
    }

    GraphNode FarNode(NodeReference reference) =>
        _nodes.TryGetValue(reference.Label, out var nodes) && nodes.TryGetValue(GraphValues.TextOf(reference.Key), out var node)
            ? node
            : new(reference, new Dictionary<string, object?>(), EventSequenceNumber.Unavailable);
}

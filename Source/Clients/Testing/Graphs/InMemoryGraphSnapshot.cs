// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Represents the nodes and edges a query reads from an in-memory graph, and how the query semantics apply to them.
/// </summary>
internal sealed class InMemoryGraphSnapshot
{
    readonly Dictionary<string, Dictionary<string, GraphNode>> _nodes;
    readonly Dictionary<string, IReadOnlyList<GraphEdge>> _edges;

    InMemoryGraphSnapshot(Dictionary<string, Dictionary<string, GraphNode>> nodes, Dictionary<string, IReadOnlyList<GraphEdge>> edges)
    {
        _nodes = nodes;
        _edges = edges;
    }

    /// <summary>
    /// Load the nodes and edges a query node needs, through every relation.
    /// </summary>
    /// <param name="graph">The <see cref="RunningGraph"/> to read.</param>
    /// <param name="root">The <see cref="GraphQueryNode"/> of the query.</param>
    /// <returns>The <see cref="InMemoryGraphSnapshot"/>.</returns>
    public static async Task<InMemoryGraphSnapshot> Load(RunningGraph graph, GraphQueryNode root)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        var edgeTypes = new HashSet<string>(StringComparer.Ordinal);
        Collect(root, labels, edgeTypes);

        var nodes = new Dictionary<string, Dictionary<string, GraphNode>>(StringComparer.Ordinal);
        foreach (var label in labels)
        {
            var page = await graph.Sink.GetNodes(label, 0, int.MaxValue);
            nodes[label] = page.Elements.ToDictionary(_ => GraphValues.TextOf(_.Reference.Key), StringComparer.Ordinal);
        }

        var edges = new Dictionary<string, IReadOnlyList<GraphEdge>>(StringComparer.Ordinal);
        foreach (var type in edgeTypes)
        {
            var page = await graph.Sink.GetEdges(type, 0, int.MaxValue);
            edges[type] = page.Elements;
        }

        return new(nodes, edges);
    }

    /// <summary>
    /// Get the roots of a query node, distinct by key and ordered by key.
    /// </summary>
    /// <param name="node">The <see cref="GraphQueryNode"/>.</param>
    /// <returns>The nodes.</returns>
    public IEnumerable<GraphNode> NodesOf(GraphQueryNode node) =>
        _nodes[node.Label].Values.OrderBy(_ => KeyOf(node, _), GraphKeys.Instance);

    /// <summary>
    /// Check whether a node satisfies a clause.
    /// </summary>
    /// <param name="node">The <see cref="GraphQueryNode"/> the clause is on.</param>
    /// <param name="element">The <see cref="GraphNode"/>.</param>
    /// <param name="clause">The <see cref="GraphQueryClause"/>.</param>
    /// <returns>True if it does, false if not.</returns>
    public bool Satisfies(GraphQueryNode node, GraphNode element, GraphQueryClause clause) => clause switch
    {
        GraphPropertyClause property => GraphKeys.AreEqual(PropertyOf(node, element, property.Property), property.Value),
        GraphRelatedClause related => Related(node, element, RelationOf(node, related.Member))
            .Any(item => related.Clauses.All(nested => Satisfies(item.Relation.Target, item.Far, nested))),
        _ => false
    };

    /// <summary>
    /// Build the row of a node.
    /// </summary>
    /// <param name="node">The <see cref="GraphQueryNode"/>.</param>
    /// <param name="element">The <see cref="GraphNode"/>.</param>
    /// <param name="edge">The <see cref="GraphEdge"/> that was followed to reach it, or null for a root.</param>
    /// <returns>The <see cref="GraphRow"/>.</returns>
    public GraphRow RowOf(GraphQueryNode node, GraphNode element, GraphEdge? edge)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var member in node.Members)
        {
            values[member.Name] = member.Kind switch
            {
                GraphViewMemberKind.Property => PropertyOf(node, element, member.Source!),
                GraphViewMemberKind.EdgeProperty => edge?.Properties.GetValueOrDefault(member.Source!),
                _ => RelationValue(node, element, member.Relation!)
            };
        }

        return new(values);
    }

    static void Collect(GraphQueryNode node, HashSet<string> labels, HashSet<string> edgeTypes)
    {
        labels.Add(node.Label);
        foreach (var relation in node.Members.Select(_ => _.Relation).OfType<GraphQueryRelation>())
        {
            edgeTypes.Add(relation.Edge);
            Collect(relation.Target, labels, edgeTypes);
        }
    }

    static GraphQueryRelation RelationOf(GraphQueryNode node, string member) =>
        node.Members.First(_ => _.Name == member).Relation!;

    static object? PropertyOf(GraphQueryNode node, GraphNode element, string property)
    {
        if (element.Properties.TryGetValue(property, out var value)) return value;

        // The key of a node is always known, also for a bare node an edge created without properties.
        return property == node.KeyProperty ? element.Reference.Key : null;
    }

    static object? KeyOf(GraphQueryNode node, GraphNode element) => PropertyOf(node, element, node.KeyProperty);

    object? RelationValue(GraphQueryNode node, GraphNode element, GraphQueryRelation relation)
    {
        var items = Related(node, element, relation).Select(_ => RowOf(relation.Target, _.Far, _.Edge));
        return relation.Cardinality == GraphViewCardinality.Many ? items.ToList() : items.FirstOrDefault();
    }

    IEnumerable<(GraphEdge Edge, GraphNode Far, GraphQueryRelation Relation)> Related(GraphQueryNode node, GraphNode element, GraphQueryRelation relation)
    {
        var key = GraphValues.TextOf(element.Reference.Key);
        var outgoing = relation.Direction == GraphViewDirection.Outgoing;
        return _edges[relation.Edge]
            .Where(edge => (outgoing ? edge.Reference.Source : edge.Reference.Target) is var near && near.Label == node.Label && GraphValues.TextOf(near.Key) == key)
            .Select(edge => (Edge: edge, Far: FarNode(outgoing ? edge.Reference.Target : edge.Reference.Source)))
            .OrderBy(_ => KeyOf(relation.Target, _.Far), GraphKeys.Instance)
            .ThenBy(_ => _.Edge.Reference.Identity, GraphKeys.Instance)
            .Select(_ => (_.Edge, _.Far, relation));
    }

    GraphNode FarNode(NodeReference reference) =>
        _nodes.TryGetValue(reference.Label, out var nodes) && nodes.TryGetValue(GraphValues.TextOf(reference.Key), out var node)
            ? node
            : new(reference, new Dictionary<string, object?>(), KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber.Unavailable);
}

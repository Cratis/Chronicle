// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a member of a <see cref="GraphView"/>.
/// </summary>
/// <param name="Name">The name of the member, after the naming policy.</param>
/// <param name="Kind">The <see cref="GraphViewMemberKind"/>.</param>
/// <param name="Source">The node or edge property the member reads, for a property or an edge property; otherwise null.</param>
/// <param name="Edge">The edge type the relation follows, for a relation; otherwise null.</param>
/// <param name="Direction">The <see cref="GraphViewDirection"/> of a relation; otherwise null.</param>
/// <param name="Cardinality">The <see cref="GraphViewCardinality"/> of a relation; otherwise null.</param>
/// <param name="View">The name of the view a relation leads to; otherwise null.</param>
public sealed record GraphViewMember(
    string Name,
    GraphViewMemberKind Kind,
    string? Source = null,
    string? Edge = null,
    GraphViewDirection? Direction = null,
    GraphViewCardinality? Cardinality = null,
    string? View = null)
{
    /// <summary>
    /// Create a member that reads a property of the root node.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <param name="source">The node property.</param>
    /// <returns>The <see cref="GraphViewMember"/>.</returns>
    public static GraphViewMember Property(string name, string source) => new(name, GraphViewMemberKind.Property, source);

    /// <summary>
    /// Create a member that reads a property of the edge that was followed.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <param name="source">The edge property.</param>
    /// <returns>The <see cref="GraphViewMember"/>.</returns>
    public static GraphViewMember EdgeProperty(string name, string source) => new(name, GraphViewMemberKind.EdgeProperty, source);

    /// <summary>
    /// Create a member that follows an edge to another view.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <param name="edge">The edge type.</param>
    /// <param name="direction">The <see cref="GraphViewDirection"/>.</param>
    /// <param name="cardinality">The <see cref="GraphViewCardinality"/>.</param>
    /// <param name="view">The name of the target view.</param>
    /// <returns>The <see cref="GraphViewMember"/>.</returns>
    public static GraphViewMember Relation(string name, string edge, GraphViewDirection direction, GraphViewCardinality cardinality, string view) =>
        new(name, GraphViewMemberKind.Relation, Edge: edge, Direction: direction, Cardinality: cardinality, View: view);
}

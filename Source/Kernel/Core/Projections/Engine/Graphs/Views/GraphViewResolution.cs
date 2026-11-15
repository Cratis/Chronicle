// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Resolves what the members of a <see cref="GraphView"/> mean in a graph: which property they read and which way a
/// relation follows an edge.
/// </summary>
internal static class GraphViewResolution
{
    /// <summary>
    /// Find a property of a node or edge schema by name, ignoring case.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the node or edge.</param>
    /// <param name="name">The name to look for.</param>
    /// <returns>The name as the schema declares it, or null when the schema has no such property.</returns>
    internal static string? FindProperty(JsonSchema schema, string name) =>
        schema.Properties.Keys.FirstOrDefault(_ => string.Equals(_, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get the label of the node at the far end of an edge.
    /// </summary>
    /// <param name="edge">The <see cref="GraphEdgeDefinition"/>.</param>
    /// <param name="direction">The <see cref="GraphViewDirection"/> followed.</param>
    /// <returns>The label.</returns>
    internal static string FarLabel(GraphEdgeDefinition edge, GraphViewDirection direction) =>
        direction == GraphViewDirection.Outgoing ? edge.TargetLabel : edge.SourceLabel;

    /// <summary>
    /// Resolve the direction a relation follows an edge.
    /// </summary>
    /// <param name="edge">The <see cref="GraphEdgeDefinition"/>.</param>
    /// <param name="root">The label of the root of the view.</param>
    /// <param name="explicitDirection">The direction the member gives, if any.</param>
    /// <param name="reason">The <see cref="GraphViewReason"/> when it cannot be resolved.</param>
    /// <returns>The direction, or null when it cannot be resolved.</returns>
    internal static GraphViewDirection? ResolveDirection(GraphEdgeDefinition edge, string root, GraphViewDirection? explicitDirection, out string? reason)
    {
        reason = null;
        if (edge.SourceLabel != root && edge.TargetLabel != root)
        {
            reason = GraphViewReason.EdgeNotIncidentOnRoot;
            return null;
        }

        if (edge.SourceLabel == edge.TargetLabel)
        {
            if (explicitDirection is null) reason = GraphViewReason.AmbiguousDirection;
            return explicitDirection;
        }

        var derived = edge.SourceLabel == root ? GraphViewDirection.Outgoing : GraphViewDirection.Incoming;
        if (explicitDirection is not null && explicitDirection != derived)
        {
            reason = GraphViewReason.EdgeNotIncidentOnRoot;
            return null;
        }

        return derived;
    }
}

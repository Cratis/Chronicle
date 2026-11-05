// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents everything a graph projection does for one event type.
/// </summary>
public class GraphEventMappings
{
    /// <summary>
    /// Gets the node mappings.
    /// </summary>
    public IList<GraphNodeMapping> Nodes { get; } = new List<GraphNodeMapping>();

    /// <summary>
    /// Gets the edge mappings.
    /// </summary>
    public IList<GraphEdgeMapping> Edges { get; } = new List<GraphEdgeMapping>();

    /// <summary>
    /// Gets the edge removals.
    /// </summary>
    public IList<GraphEdgeRemoval> EdgeRemovals { get; } = new List<GraphEdgeRemoval>();

    /// <summary>
    /// Gets the node removals.
    /// </summary>
    public IList<GraphNodeRemoval> NodeRemovals { get; } = new List<GraphNodeRemoval>();
}

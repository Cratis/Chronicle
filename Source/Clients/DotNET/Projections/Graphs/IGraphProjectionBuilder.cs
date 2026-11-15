// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines a builder for the nodes and edges of a graph projection.
/// </summary>
public interface IGraphProjectionBuilder
{
    /// <summary>
    /// Define a node.
    /// </summary>
    /// <typeparam name="TNode">The type of node. Its name is the label of the node.</typeparam>
    /// <param name="configure">Callback that configures the node.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphProjectionBuilder Node<TNode>(Action<IGraphNodeBuilder<TNode>> configure);

    /// <summary>
    /// Define an edge.
    /// </summary>
    /// <typeparam name="TEdge">The type of edge. Its properties, apart from the ones holding the keys of the end nodes, are the properties of the edge.</typeparam>
    /// <typeparam name="TSource">The type of the node the edge starts at.</typeparam>
    /// <typeparam name="TTarget">The type of the node the edge ends at.</typeparam>
    /// <param name="configure">Callback that configures the edge.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphProjectionBuilder Edge<TEdge, TSource, TTarget>(Action<IGraphEdgeBuilder<TEdge, TSource, TTarget>> configure);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines a builder for an edge.
/// </summary>
/// <typeparam name="TEdge">The type of edge.</typeparam>
/// <typeparam name="TSource">The type of the node the edge starts at.</typeparam>
/// <typeparam name="TTarget">The type of the node the edge ends at.</typeparam>
public interface IGraphEdgeBuilder<TEdge, TSource, TTarget>
{
    /// <summary>
    /// Name the edge type. Not needed when the type of the edge has the edge attribute.
    /// </summary>
    /// <param name="type">The edge type, for example MEMBER_OF.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeBuilder<TEdge, TSource, TTarget> Named(string type);

    /// <summary>
    /// Declare the property that identifies the edge among edges of the same type between the same two nodes.
    /// Not needed when the property has the key attribute.
    /// </summary>
    /// <typeparam name="TIdentity">The type of the identity.</typeparam>
    /// <param name="property">Expression selecting the identifying property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeBuilder<TEdge, TSource, TTarget> IdentifiedBy<TIdentity>(Expression<Func<TEdge, TIdentity>> property);

    /// <summary>
    /// Create or update the edge when an event is observed. Both end nodes are created when they do not exist.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="configure">Callback for the keys of the end nodes and the properties.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeBuilder<TEdge, TSource, TTarget> From<TEvent>(Action<IGraphEdgeFromBuilder<TEvent, TEdge>> configure);

    /// <summary>
    /// Delete the edge when an event is observed.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="configure">Callback for the keys of the end nodes.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeBuilder<TEdge, TSource, TTarget> RemovedWith<TEvent>(Action<IGraphEdgeRemovedWithBuilder<TEvent>> configure);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the builder for how an event creates or updates an edge.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TEdge">The type of edge.</typeparam>
public interface IGraphEdgeFromBuilder<TEvent, TEdge> : IGraphMappingBuilder<TEvent, TEdge, IGraphEdgeFromBuilder<TEvent, TEdge>>
{
    /// <summary>
    /// Take the key of the source node from a property on the event.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeFromBuilder<TEvent, TEdge> UsingSourceKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Take the key of the target node from a property on the event.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeFromBuilder<TEvent, TEdge> UsingTargetKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Take the identity of the edge from a property on the event, for an edge type with an identifying property.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeFromBuilder<TEvent, TEdge> UsingIdentity(Expression<Func<TEvent, object?>> property);
}

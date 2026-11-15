// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the builder for how an event deletes an edge.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
public interface IGraphEdgeRemovedWithBuilder<TEvent>
{
    /// <summary>
    /// Take the key of the source node from a property on the event.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeRemovedWithBuilder<TEvent> UsingSourceKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Take the key of the target node from a property on the event.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeRemovedWithBuilder<TEvent> UsingTargetKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Take the identity of the edge from a property on the event, for an edge type with an identifying property.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphEdgeRemovedWithBuilder<TEvent> UsingIdentity(Expression<Func<TEvent, object?>> property);
}

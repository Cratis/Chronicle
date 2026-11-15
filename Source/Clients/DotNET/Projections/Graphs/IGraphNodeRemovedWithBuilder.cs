// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the builder for how an event deletes a node.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
public interface IGraphNodeRemovedWithBuilder<TEvent>
{
    /// <summary>
    /// Take the key of the node from a property on the event. By default it is the event source id.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeRemovedWithBuilder<TEvent> UsingKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Delete the edges attached to the node with it. Without it, a node that still has edges cannot be deleted.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeRemovedWithBuilder<TEvent> Detach();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the builder for how an event creates or updates a node.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TNode">The type of node.</typeparam>
public interface IGraphNodeFromBuilder<TEvent, TNode> : IGraphMappingBuilder<TEvent, TNode, IGraphNodeFromBuilder<TEvent, TNode>>
{
    /// <summary>
    /// Take the key of the node from a property on the event. By default it is the event source id.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeFromBuilder<TEvent, TNode> UsingKey(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Only set properties on a node that already exists, instead of creating it.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeFromBuilder<TEvent, TNode> OnlyWhenExisting();
}

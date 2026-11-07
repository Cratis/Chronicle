// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines a builder for a node.
/// </summary>
/// <typeparam name="TNode">The type of node.</typeparam>
public interface IGraphNodeBuilder<TNode>
{
    /// <summary>
    /// Declare the property that holds the unique key of the node. Not needed when the property has the key attribute.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="property">Expression selecting the key property.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeBuilder<TNode> IdentifiedBy<TKey>(Expression<Func<TNode, TKey>> property);

    /// <summary>
    /// Create or update the node when an event is observed.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="configure">Optional callback for the key and the properties. By default the key is the event source id and properties are mapped by name.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeBuilder<TNode> From<TEvent>(Action<IGraphNodeFromBuilder<TEvent, TNode>>? configure = null);

    /// <summary>
    /// Delete the node when an event is observed.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="configure">Optional callback for the key and for detaching. By default the key is the event source id.</param>
    /// <returns>The builder for continuation.</returns>
    IGraphNodeBuilder<TNode> RemovedWith<TEvent>(Action<IGraphNodeRemovedWithBuilder<TEvent>>? configure = null);
}

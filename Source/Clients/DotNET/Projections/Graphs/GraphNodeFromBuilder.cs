// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphNodeFromBuilder{TEvent, TNode}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TNode">The type of node.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphNodeFromBuilder<TEvent, TNode>(INamingPolicy namingPolicy)
    : GraphMappingBuilder<TEvent, TNode, IGraphNodeFromBuilder<TEvent, TNode>>(namingPolicy), IGraphNodeFromBuilder<TEvent, TNode>
{
    /// <summary>
    /// Gets the key expression, or null for the event source id.
    /// </summary>
    public string? Key { get; private set; }

    /// <summary>
    /// Gets whether the mapping only sets properties on an existing node.
    /// </summary>
    public bool UpdateOnly { get; private set; }

    /// <inheritdoc/>
    protected override IGraphNodeFromBuilder<TEvent, TNode> Self => this;

    /// <inheritdoc/>
    public IGraphNodeFromBuilder<TEvent, TNode> UsingKey(Expression<Func<TEvent, object?>> property)
    {
        Key = EventProperty(property);
        return this;
    }

    /// <inheritdoc/>
    public IGraphNodeFromBuilder<TEvent, TNode> OnlyWhenExisting()
    {
        UpdateOnly = true;
        return this;
    }
}

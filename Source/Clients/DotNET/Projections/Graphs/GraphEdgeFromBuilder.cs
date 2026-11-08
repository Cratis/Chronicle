// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphEdgeFromBuilder{TEvent, TEdge}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TEdge">The type of edge.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphEdgeFromBuilder<TEvent, TEdge>(INamingPolicy namingPolicy)
    : GraphMappingBuilder<TEvent, TEdge, IGraphEdgeFromBuilder<TEvent, TEdge>>(namingPolicy), IGraphEdgeFromBuilder<TEvent, TEdge>
{
    /// <summary>
    /// Gets the source key expression.
    /// </summary>
    public string? SourceKey { get; private set; }

    /// <summary>
    /// Gets the target key expression.
    /// </summary>
    public string? TargetKey { get; private set; }

    /// <summary>
    /// Gets the identity expression.
    /// </summary>
    public string? Identity { get; private set; }

    /// <inheritdoc/>
    protected override IGraphEdgeFromBuilder<TEvent, TEdge> Self => this;

    /// <inheritdoc/>
    public IGraphEdgeFromBuilder<TEvent, TEdge> UsingSourceKey(Expression<Func<TEvent, object?>> property)
    {
        SourceKey = EventProperty(property);
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeFromBuilder<TEvent, TEdge> UsingTargetKey(Expression<Func<TEvent, object?>> property)
    {
        TargetKey = EventProperty(property);
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeFromBuilder<TEvent, TEdge> UsingIdentity(Expression<Func<TEvent, object?>> property)
    {
        Identity = EventProperty(property);
        return this;
    }
}

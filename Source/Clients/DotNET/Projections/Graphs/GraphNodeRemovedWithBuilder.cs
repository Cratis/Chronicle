// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphNodeRemovedWithBuilder{TEvent}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphNodeRemovedWithBuilder<TEvent>(INamingPolicy namingPolicy) : IGraphNodeRemovedWithBuilder<TEvent>
{
    /// <summary>
    /// Gets the key expression, or null for the event source id.
    /// </summary>
    public string? Key { get; private set; }

    /// <summary>
    /// Gets whether the edges of the node are deleted with it.
    /// </summary>
    public bool DetachEdges { get; private set; }

    /// <inheritdoc/>
    public IGraphNodeRemovedWithBuilder<TEvent> UsingKey(Expression<Func<TEvent, object?>> property)
    {
        Key = (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Source);
        return this;
    }

    /// <inheritdoc/>
    public IGraphNodeRemovedWithBuilder<TEvent> Detach()
    {
        DetachEdges = true;
        return this;
    }
}

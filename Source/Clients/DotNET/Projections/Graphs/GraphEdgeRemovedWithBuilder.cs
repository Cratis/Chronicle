// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphEdgeRemovedWithBuilder{TEvent}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphEdgeRemovedWithBuilder<TEvent>(INamingPolicy namingPolicy) : IGraphEdgeRemovedWithBuilder<TEvent>
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
    public IGraphEdgeRemovedWithBuilder<TEvent> UsingSourceKey(Expression<Func<TEvent, object?>> property)
    {
        SourceKey = Resolve(property);
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeRemovedWithBuilder<TEvent> UsingTargetKey(Expression<Func<TEvent, object?>> property)
    {
        TargetKey = Resolve(property);
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeRemovedWithBuilder<TEvent> UsingIdentity(Expression<Func<TEvent, object?>> property)
    {
        Identity = Resolve(property);
        return this;
    }

    string Resolve(Expression<Func<TEvent, object?>> property) => (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Source);
}

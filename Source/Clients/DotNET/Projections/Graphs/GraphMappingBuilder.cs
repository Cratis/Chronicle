// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Collects the property mappings of a node or edge mapping, shared by the fluent builders.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TElement">The type of node or edge.</typeparam>
/// <typeparam name="TSelf">The type of the concrete builder.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal abstract class GraphMappingBuilder<TEvent, TElement, TSelf>(INamingPolicy namingPolicy) : IGraphMappingBuilder<TEvent, TElement, TSelf>
    where TSelf : class
{
    /// <summary>
    /// Gets the explicit property expressions.
    /// </summary>
    public Dictionary<string, string> Properties { get; } = [];

    /// <summary>
    /// Gets the self reference for continuation.
    /// </summary>
    protected abstract TSelf Self { get; }

    /// <summary>
    /// Gets the <see cref="INamingPolicy"/>.
    /// </summary>
    protected INamingPolicy NamingPolicy { get; } = namingPolicy;

    /// <inheritdoc/>
    public IGraphSetBuilder<TEvent, TSelf> Set(Expression<Func<TElement, object?>> property) => new GraphSetBuilder<TEvent, TSelf>(Self, NamingPolicy, NameOf(property), Properties, null);

    /// <inheritdoc/>
    public IGraphSetBuilder<TEvent, TSelf> Add(Expression<Func<TElement, object?>> property) => new GraphSetBuilder<TEvent, TSelf>(Self, NamingPolicy, NameOf(property), Properties, WellKnownExpressions.Add);

    /// <inheritdoc/>
    public IGraphSetBuilder<TEvent, TSelf> Subtract(Expression<Func<TElement, object?>> property) => new GraphSetBuilder<TEvent, TSelf>(Self, NamingPolicy, NameOf(property), Properties, WellKnownExpressions.Subtract);

    /// <inheritdoc/>
    public TSelf Increment(Expression<Func<TElement, object?>> property) => Constant(property, WellKnownExpressions.Increment);

    /// <inheritdoc/>
    public TSelf Decrement(Expression<Func<TElement, object?>> property) => Constant(property, WellKnownExpressions.Decrement);

    /// <inheritdoc/>
    public TSelf Count(Expression<Func<TElement, object?>> property) => Constant(property, WellKnownExpressions.Count);

    /// <summary>
    /// Resolve the expression for a property on the event.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The projection expression for the property.</returns>
    protected string EventProperty(Expression<Func<TEvent, object?>> property) =>
        (string)ProjectionPropertyPaths.FromExpression(property, NamingPolicy, ProjectionPathRole.Source);

    string NameOf(Expression<Func<TElement, object?>> property) =>
        (string)ProjectionPropertyPaths.FromExpression(property, NamingPolicy, ProjectionPathRole.Target);

    TSelf Constant(Expression<Func<TElement, object?>> property, string expression)
    {
        Properties[NameOf(property)] = expression;
        return Self;
    }
}

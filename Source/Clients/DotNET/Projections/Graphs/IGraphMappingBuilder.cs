// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the property mappings shared by nodes and edges.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TElement">The type of the node or edge.</typeparam>
/// <typeparam name="TSelf">The type of the builder, for continuation.</typeparam>
public interface IGraphMappingBuilder<TEvent, TElement, TSelf>
{
    /// <summary>
    /// Set a property.
    /// </summary>
    /// <param name="property">Expression selecting the property.</param>
    /// <returns>The builder for what to set it to.</returns>
    IGraphSetBuilder<TEvent, TSelf> Set(Expression<Func<TElement, object?>> property);

    /// <summary>
    /// Add the value of an event property to a counter property.
    /// </summary>
    /// <param name="property">Expression selecting the counter property.</param>
    /// <returns>The builder for which event property to add.</returns>
    IGraphSetBuilder<TEvent, TSelf> Add(Expression<Func<TElement, object?>> property);

    /// <summary>
    /// Subtract the value of an event property from a counter property.
    /// </summary>
    /// <param name="property">Expression selecting the counter property.</param>
    /// <returns>The builder for which event property to subtract.</returns>
    IGraphSetBuilder<TEvent, TSelf> Subtract(Expression<Func<TElement, object?>> property);

    /// <summary>
    /// Increment a counter property by one.
    /// </summary>
    /// <param name="property">Expression selecting the counter property.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf Increment(Expression<Func<TElement, object?>> property);

    /// <summary>
    /// Decrement a counter property by one.
    /// </summary>
    /// <param name="property">Expression selecting the counter property.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf Decrement(Expression<Func<TElement, object?>> property);

    /// <summary>
    /// Count the events on a counter property.
    /// </summary>
    /// <param name="property">Expression selecting the counter property.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf Count(Expression<Func<TElement, object?>> property);
}

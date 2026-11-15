// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines the builder for what a property is set to.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TSelf">The type of the builder, for continuation.</typeparam>
public interface IGraphSetBuilder<TEvent, TSelf>
{
    /// <summary>
    /// Take the value from a property on the event. For add and subtract, the property whose value is added or subtracted.
    /// </summary>
    /// <param name="property">Expression selecting the event property.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf ToEventProperty(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Take the value from a property on the event context, for example when it occurred.
    /// </summary>
    /// <param name="property">Expression selecting the event context property.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf ToEventContextProperty(Expression<Func<EventContext, object?>> property);

    /// <summary>
    /// Set a constant value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The builder for continuation.</returns>
    TSelf ToValue(object? value);
}

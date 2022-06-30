// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a builder for building set operations for properties - represented as expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
public interface ISetBuilder<TModel, TEvent, TProperty> : IPropertyExpressionBuilder
{
    /// <summary>
    /// Straight map to a property on the event.
    /// </summary>
    /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
    /// <returns>Builder continuation.</returns>
    IFromBuilder<TModel, TEvent> To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor);

    /// <summary>
    /// Map to the event source id on the metadata of the event.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IFromBuilder<TModel, TEvent> ToEventSourceId();

    /// <summary>
    /// Map to a property on the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="eventContextPropertyAccessor">Property accessor for specifying which property to map to.</param>
    /// <returns>Builder continuation.</returns>
    IFromBuilder<TModel, TEvent> ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor);
}

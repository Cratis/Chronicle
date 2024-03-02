// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Defines a builder for building set operations for properties - represented as expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public interface ISetBuilder<TModel, TEvent, TParentBuilder> : IPropertyExpressionBuilder
{
    /// <summary>
    /// Straight map to a property on the event, based on the <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to map to.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder To(PropertyPath propertyPath);

    /// <summary>
    /// Map to the event source id on the metadata of the event.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    TParentBuilder ToEventSourceId();
}

/// <summary>
/// Defines a builder for building set operations for properties - represented as expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public interface ISetBuilder<TModel, TEvent, TProperty, TParentBuilder> : ISetBuilder<TModel, TEvent, TParentBuilder>
{
    /// <summary>
    /// Set the property to a specific value.
    /// </summary>
    /// <param name="value">Value to set.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder ToValue(TProperty value);

    /// <summary>
    /// Straight map to a property on the event.
    /// </summary>
    /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor);

    /// <summary>
    /// Map to a property on the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="eventContextPropertyAccessor">Property accessor for specifying which property to map to.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor);
}

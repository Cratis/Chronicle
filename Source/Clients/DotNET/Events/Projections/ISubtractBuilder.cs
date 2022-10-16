// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a builder for building subtract operations for properties - represented as expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public interface ISubtractBuilder<TModel, TEvent, TProperty, TParentBuilder> : IPropertyExpressionBuilder
    where TParentBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TParentBuilder>
{
    /// <summary>
    /// Add with a property on the event.
    /// </summary>
    /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor);
}

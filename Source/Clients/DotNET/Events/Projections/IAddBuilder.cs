// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a builder for building add operations for properties - represented as expressions.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    /// <typeparam name="TProperty">The type of the property we're targetting.</typeparam>
    public interface IAddBuilder<TModel, TEvent, TProperty> : IPropertyExpressionBuilder
    {
        /// <summary>
        /// Add with a property on the event.
        /// </summary>
        /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
        /// <returns>Builder continuation.</returns>
        IFromBuilder<TModel, TEvent> With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor);
    }
}

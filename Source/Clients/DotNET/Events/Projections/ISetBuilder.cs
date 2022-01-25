// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a builder for building set operations for properties - represented as expressions.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    /// <typeparam name="TProperty">The type of the property we're targetting.</typeparam>
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
    }
}

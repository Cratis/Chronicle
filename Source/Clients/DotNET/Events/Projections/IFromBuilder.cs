// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the builder for building from expressions.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    public interface IFromBuilder<TModel, TEvent>
    {
        /// <summary>
        /// Define what key to use. This is optional, if not set - it will default to using the event source identifier on the event.
        /// </summary>
        /// <param name="keyAccessor">Accessor for the property to use.</param>
        /// <returns>Builder continuation</returns>
        IFromBuilder<TModel, TEvent> UsingKey(Expression<Func<TEvent, object>> keyAccessor);

        /// <summary>
        /// Set the target property on the model to the content of the source property on an event.
        /// </summary>
        /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
        /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
        /// <returns>Builder continuation</returns>
        IFromBuilder<TModel, TEvent> Set(Expression<Func<TModel, object>> modelPropertyAccessor, Expression<Func<TEvent, object>> eventPropertyAccessor);
    }
}

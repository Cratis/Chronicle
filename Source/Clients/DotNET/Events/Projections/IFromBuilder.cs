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
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <param name="keyAccessor">Accessor for the property to use.</param>
        /// <returns>Builder continuation</returns>
        IFromBuilder<TModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

        /// <summary>
        /// Set the target property on the model to the content of the source property on an event.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
        /// <param name="eventPropertyAccessor">Event property accessor for defining the source property.</param>
        /// <returns>Builder continuation</returns>
        IFromBuilder<TModel, TEvent> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor, Expression<Func<TEvent, TProperty>> eventPropertyAccessor);

        /// <summary>
        /// Build <see cref="FromDefinition"/> from the builder.
        /// </summary>
        /// <returns>A new instance of <see cref="FromDefinition"/>.</returns>
        FromDefinition Build();
    }
}

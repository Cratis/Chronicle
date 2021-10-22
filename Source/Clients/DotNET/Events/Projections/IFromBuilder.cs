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
        /// Start building the add operation to a target property on the model.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
        /// <returns>Builder continuation</returns>
        IAddBuilder<TModel, TEvent, TProperty> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

        /// <summary>
        /// Start building the set operation to a target property on the model.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property.</typeparam>
        /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
        /// <returns>Builder continuation</returns>
        ISetBuilder<TModel, TEvent, TProperty> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

        /// <summary>
        /// Build <see cref="FromDefinition"/> from the builder.
        /// </summary>
        /// <returns>A new instance of <see cref="FromDefinition"/>.</returns>
        FromDefinition Build();
    }
}

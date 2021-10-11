// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Reflection;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IFromBuilder{TModel, TEvent}"/>.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    public class FromBuilder<TModel, TEvent> : IFromBuilder<TModel, TEvent>
    {
        readonly FromDefinition _propertyMaps = new();

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> UsingKey(Expression<Func<TEvent, object>> keyAccessor)
        {
            return this;
        }

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> Set(Expression<Func<TModel, object>> modelPropertyAccessor, Expression<Func<TEvent, object>> eventPropertyAccessor)
        {
            _propertyMaps[modelPropertyAccessor.GetPropertyInfo().Name] = eventPropertyAccessor.GetPropertyInfo().Name;
            return this;
        }

        /// <inheritdoc/>
        public FromDefinition Build()
        {
            return _propertyMaps;
        }
    }
}

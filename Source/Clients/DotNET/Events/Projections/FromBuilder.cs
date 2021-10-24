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
        readonly List<IPropertyExpressionBuilder> _propertyExpressions = new();

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
        {
            return this;
        }

        /// <inheritdoc/>
        public IAddBuilder<TModel, TEvent, TProperty> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
        {
            var addBuilder = new AddBuilder<TModel, TEvent, TProperty>(this, modelPropertyAccessor.GetPropertyInfo().Name);
            _propertyExpressions.Add(addBuilder);
            return addBuilder;
        }

        /// <inheritdoc/>
        public ISubtractBuilder<TModel, TEvent, TProperty> Subtract<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
        {
            var subtractBuilder = new SubtractBuilder<TModel, TEvent, TProperty>(this, modelPropertyAccessor.GetPropertyInfo().Name);
            _propertyExpressions.Add(subtractBuilder);
            return subtractBuilder;
        }

        /// <inheritdoc/>
        public ISetBuilder<TModel, TEvent, TProperty> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
        {
            var setBuilder = new SetBuilder<TModel, TEvent, TProperty>(this, modelPropertyAccessor.GetPropertyInfo().Name);
            _propertyExpressions.Add(setBuilder);
            return setBuilder;
        }

        /// <inheritdoc/>
        public FromDefinition Build() => new(_propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()));
    }
}

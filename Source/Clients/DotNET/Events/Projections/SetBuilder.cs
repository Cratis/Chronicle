// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Properties;
using Cratis.Reflection;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="ISetBuilder{TModel, TEvent, TProperty}"/>.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    /// <typeparam name="TProperty">The type of the property we're targetting.</typeparam>
    public class SetBuilder<TModel, TEvent, TProperty> : ISetBuilder<TModel, TEvent, TProperty>
    {
        readonly IFromBuilder<TModel, TEvent> _parent;
        string _expression = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty}"/> class.
        /// </summary>
        /// <param name="parent">Parent builder.</param>
        /// <param name="targetProperty">Target property we're building for.</param>
        public SetBuilder(IFromBuilder<TModel, TEvent> parent, PropertyPath targetProperty)
        {
            _parent = parent;
            TargetProperty = targetProperty;
        }

        /// <inheritdoc/>
        public PropertyPath TargetProperty { get; }

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
        {
            _expression = eventPropertyAccessor.GetPropertyInfo().Name;
            return _parent;
        }

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> ToEventSourceId()
        {
            _expression = "$eventSourceId";
            return _parent;
        }

        /// <inheritdoc/>
        public string Build()
        {
            return _expression;
        }
    }
}


// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IAddBuilder{TModel, TEvent, TProperty}"/>.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TEvent">Event to build for.</typeparam>
    /// <typeparam name="TProperty">The type of the property we're targetting.</typeparam>
    public class SubtractBuilder<TModel, TEvent, TProperty> : ISubtractBuilder<TModel, TEvent, TProperty>
    {
        readonly IFromBuilder<TModel, TEvent> _parent;
        string _expression = string.Empty;

        /// <inheritdoc/>
        public PropertyPath TargetProperty { get; }

        /// <summary>
        /// /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty}"/> class.
        /// </summary>
        /// <param name="parent">Parent builder.</param>
        /// <param name="targetProperty">Target property we're building for.</param>
        public SubtractBuilder(IFromBuilder<TModel, TEvent> parent, PropertyPath targetProperty)
        {
            _parent = parent;
            TargetProperty = targetProperty;
        }

        /// <inheritdoc/>
        public IFromBuilder<TModel, TEvent> With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
        {
            _expression = $"$subtract({eventPropertyAccessor.GetPropertyPath()})";
            return _parent;
        }

        /// <inheritdoc/>
        public string Build()
        {
            return _expression;
        }
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAddBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class AddBuilder<TModel, TEvent, TProperty, TParentBuilder> : IAddBuilder<TModel, TEvent, TProperty, TParentBuilder>
    where TParentBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TParentBuilder>
{
    readonly TParentBuilder _parent;
    string _expression = string.Empty;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    public AddBuilder(TParentBuilder parent, PropertyPath targetProperty)
    {
        _parent = parent;
        TargetProperty = targetProperty;
    }

    /// <inheritdoc/>
    public TParentBuilder With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = $"$add({eventPropertyAccessor.GetPropertyPath()})";
        return _parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        return _expression;
    }
}

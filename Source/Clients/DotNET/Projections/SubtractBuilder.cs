// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Properties;
using Cratis.Reflection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISubtractBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class SubtractBuilder<TModel, TEvent, TProperty, TParentBuilder> : ISubtractBuilder<TModel, TEvent, TProperty, TParentBuilder>
    where TParentBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TParentBuilder>
{
    readonly TParentBuilder _parent;
    string _expression = string.Empty;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="SubtractBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    public SubtractBuilder(TParentBuilder parent, PropertyPath targetProperty)
    {
        _parent = parent;
        TargetProperty = targetProperty;
    }

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <inheritdoc/>
    public TParentBuilder With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = $"$subtract({eventPropertyAccessor.GetPropertyPath()})";
        return _parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        ThrowIfMissingSubtractWithExpression();

        return _expression;
    }

    void ThrowIfMissingSubtractWithExpression()
    {
        if (string.IsNullOrEmpty(_expression))
        {
            throw new MissingSubtractWithExpression(typeof(TModel), TargetProperty);
        }
    }
}

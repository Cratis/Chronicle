// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAddBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AddBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
/// </remarks>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
public class AddBuilder<TModel, TEvent, TProperty, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty) : IAddBuilder<TModel, TEvent, TProperty, TParentBuilder>
    where TParentBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TParentBuilder>
{
    string _expression = string.Empty;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public TParentBuilder With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = $"$add({eventPropertyAccessor.GetPropertyPath()})";
        return parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        ThrowIfMissingAddWithExpression();

        return _expression;
    }

    void ThrowIfMissingAddWithExpression()
    {
        if (string.IsNullOrEmpty(_expression))
        {
            throw new MissingAddWithExpression(typeof(TModel), TargetProperty);
        }
    }
}

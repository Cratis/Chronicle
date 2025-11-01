// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISubtractBuilder{TReadModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for property names.</param>
public class SubtractBuilder<TReadModel, TEvent, TProperty, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy) : ISubtractBuilder<TReadModel, TEvent, TProperty, TParentBuilder>
    where TParentBuilder : class, IReadModelPropertiesBuilder<TReadModel, TEvent, TParentBuilder>
{
    string _expression = string.Empty;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public TParentBuilder With(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = $"$subtract({namingPolicy.GetPropertyName(eventPropertyAccessor.GetPropertyPath())})";
        return parent;
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
            throw new MissingSubtractWithExpression(typeof(TReadModel), TargetProperty);
        }
    }
}

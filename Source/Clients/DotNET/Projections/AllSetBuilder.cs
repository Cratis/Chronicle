// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Events;
using Cratis.Projections.Expressions;
using Cratis.Properties;
using Cratis.Reflection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAllSetBuilder{TModel, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
/// </remarks>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
public class AllSetBuilder<TModel, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty) : IAllSetBuilder<TModel, TParentBuilder>
{
    IEventValueExpression? _expression;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public TParentBuilder ToEventSourceId()
    {
        _expression = new EventSourceIdExpression();
        return parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
    {
        _expression = new EventContextPropertyExpression(eventContextPropertyAccessor.GetPropertyPath());
        return parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        ThrowIfMissingToExpression();

        return _expression!.Build();
    }

    void ThrowIfMissingToExpression()
    {
        if (_expression is null)
        {
            throw new MissingToExpressionForAllSet(typeof(TModel), TargetProperty);
        }
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAllSetBuilder{TModel, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class AllSetBuilder<TModel, TParentBuilder> : IAllSetBuilder<TModel, TParentBuilder>
{
    readonly TParentBuilder _parent;
    IEventValueExpression? _expression;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    public AllSetBuilder(TParentBuilder parent, PropertyPath targetProperty)
    {
        _parent = parent;
        TargetProperty = targetProperty;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventSourceId()
    {
        _expression = new EventSourceIdExpression();
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
    {
        _expression = new EventContextPropertyExpression(eventContextPropertyAccessor.GetPropertyPath());
        return _parent;
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

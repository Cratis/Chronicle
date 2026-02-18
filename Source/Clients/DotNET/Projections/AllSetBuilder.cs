// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAllSetBuilder{TReadModel, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for property names.</param>
public class AllSetBuilder<TReadModel, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy) : IAllSetBuilder<TReadModel, TParentBuilder>
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
        _expression = new EventContextPropertyExpression(namingPolicy.GetPropertyName(eventContextPropertyAccessor.GetPropertyPath()));
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
            throw new MissingToExpressionForAllSet(typeof(TReadModel), TargetProperty);
        }
    }
}

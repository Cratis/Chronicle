// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TReadModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="forceEventProperty">Whether or not to force this to have to map to a target property or not.</param>
public class SetBuilder<TReadModel, TEvent, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy, bool forceEventProperty = false)
    : ISetBuilder<TReadModel, TEvent, TParentBuilder>
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
    protected IEventValueExpression? _expression;
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public TParentBuilder To(PropertyPath propertyPath)
    {
        _expression = new EventContentPropertyExpression(ProjectionPropertyPaths.Resolve(propertyPath, namingPolicy, ProjectionPathRole.Source));
        return parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventSourceId()
    {
        ThrowIfOnlyEventPropertyIsSupported();
        _expression = new EventSourceIdExpression();
        return parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        ThrowIfMissingToExpression();

        return _expression!.Build();
    }

    /// <summary>
    /// Converts an accessor using this builder's serialization naming policy.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="description">The description of the property being configured.</param>
    /// <returns>The serialized property path.</returns>
    private protected PropertyPath ResolvePropertyPath(LambdaExpression expression, string description) =>
        ProjectionPropertyPaths.FromExpression(expression, namingPolicy, ProjectionPathRole.Source, description);

    void ThrowIfMissingToExpression()
    {
        if (_expression is null)
        {
            throw new MissingToExpression(typeof(TReadModel), typeof(TEvent), TargetProperty);
        }
    }

    void ThrowIfOnlyEventPropertyIsSupported()
    {
        if (forceEventProperty)
        {
            throw new OnlyEventPropertySupported(TargetProperty);
        }
    }
}

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TReadModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SetBuilder{TReadModel, TEvent, TProperty, TParentBuilder}"/> class.
/// </remarks>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property we're building for.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use property names.</param>
/// <param name="forceEventProperty">Whether or not to force this to have to map to a target property or not.</param>
public class SetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy, bool forceEventProperty = false)
    : SetBuilder<TReadModel, TEvent, TParentBuilder>(parent, targetProperty, namingPolicy, forceEventProperty), ISetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>
{
    readonly TParentBuilder _parent = parent;

    /// <inheritdoc/>
    public TParentBuilder ToValue(TProperty value)
    {
        _expression = ConstantValueExpression.From(value);
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        var propertyPath = ResolvePropertyPath(eventPropertyAccessor, $"property '{TargetProperty}' on read model '{typeof(TReadModel).FullName}'");
        _expression = new EventContentPropertyExpression(propertyPath);
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
    {
        var propertyPath = ResolvePropertyPath(eventContextPropertyAccessor, $"the event context value for property '{TargetProperty}' on read model '{typeof(TReadModel).FullName}'");
        _expression = new EventContextPropertyExpression(propertyPath);
        return _parent;
    }
}

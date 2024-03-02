// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq.Expressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class SetBuilder<TModel, TEvent, TParentBuilder> : ISetBuilder<TModel, TEvent, TParentBuilder>
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly TParentBuilder _parent;
    protected IEventValueExpression? _expression;
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields
    readonly bool _forceEventProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    /// <param name="forceEventProperty">Whether or not to force this to have to map to a target property or not.</param>
    public SetBuilder(TParentBuilder parent, PropertyPath targetProperty, bool forceEventProperty = false)
    {
        _parent = parent;
        TargetProperty = targetProperty;
        _forceEventProperty = forceEventProperty;
    }

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <inheritdoc/>
    public TParentBuilder To(PropertyPath propertyPath)
    {
        _expression = new EventContentPropertyExpression(propertyPath);
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventSourceId()
    {
        ThrowIfOnlyEventPropertyIsSupported();
        _expression = new EventSourceIdExpression();
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
            throw new MissingToExpression(typeof(TModel), typeof(TEvent), TargetProperty);
        }
    }

    void ThrowIfOnlyEventPropertyIsSupported()
    {
        if (_forceEventProperty)
        {
            throw new OnlyEventPropertySupported(TargetProperty);
        }
    }
}

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class SetBuilder<TModel, TEvent, TProperty, TParentBuilder> : SetBuilder<TModel, TEvent, TParentBuilder>, ISetBuilder<TModel, TEvent, TProperty, TParentBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    /// <param name="forceEventProperty">Whether or not to force this to have to map to a target property or not.</param>
    public SetBuilder(TParentBuilder parent, PropertyPath targetProperty, bool forceEventProperty = false)
        : base(parent, targetProperty, forceEventProperty)
    {
    }

    /// <inheritdoc/>
    public TParentBuilder ToValue(TProperty value)
    {
        if (value is null)
        {
            _expression = ValueExpression.Null;
            return _parent;
        }

        object actualValue = value!;

        if (actualValue.IsConcept())
        {
            actualValue = actualValue.GetConceptValue();
        }

        if (actualValue.GetType().IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(actualValue.GetType());
            if (underlyingType == typeof(int))
            {
                actualValue = Convert.ChangeType(actualValue, underlyingType);
            }
            else
            {
                actualValue = actualValue.ToString()!;
            }
        }

        var invariantString = actualValue switch
        {
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("o", CultureInfo.InvariantCulture),
            _ => string.Create(CultureInfo.InvariantCulture, $"{actualValue}")
        };

        _expression = new ValueExpression(invariantString);
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = new EventContentPropertyExpression(eventPropertyAccessor.GetPropertyPath());
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
    {
        _expression = new EventContextPropertyExpression(eventContextPropertyAccessor.GetPropertyPath());
        return _parent;
    }
}

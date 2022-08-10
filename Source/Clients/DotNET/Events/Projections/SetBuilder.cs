// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TModel, TEvent, TProperty}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
public class SetBuilder<TModel, TEvent, TProperty> : ISetBuilder<TModel, TEvent, TProperty>
{
    readonly IFromBuilder<TModel, TEvent> _parent;
    readonly bool _forceEventProperty;
    string _expression = string.Empty;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetBuilder{TModel, TEvent, TProperty}"/> class.
    /// </summary>
    /// <param name="parent">Parent builder.</param>
    /// <param name="targetProperty">Target property we're building for.</param>
    /// <param name="forceEventProperty">Whether or not to force this to have to map to a target property or not.</param>
    public SetBuilder(IFromBuilder<TModel, TEvent> parent, PropertyPath targetProperty, bool forceEventProperty = false)
    {
        _parent = parent;
        TargetProperty = targetProperty;
        _forceEventProperty = forceEventProperty;
    }

    /// <inheritdoc/>
    public IFromBuilder<TModel, TEvent> To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = eventPropertyAccessor.GetPropertyPath();
        return _parent;
    }

    /// <inheritdoc/>
    public IFromBuilder<TModel, TEvent> ToEventSourceId()
    {
        ThrowIfOnlyEventPropertyIsSupported();

        _expression = "$eventSourceId";
        return _parent;
    }

    /// <inheritdoc/>
    public IFromBuilder<TModel, TEvent> ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
    {
        var property = eventContextPropertyAccessor.GetPropertyPath();
        _expression = $"$eventContext({property})";

        return _parent;
    }

    /// <inheritdoc/>
    public string Build()
    {
        return _expression;
    }

    void ThrowIfOnlyEventPropertyIsSupported()
    {
        if (_forceEventProperty)
        {
            throw new OnlyEventPropertySupported(TargetProperty);
        }
    }
}

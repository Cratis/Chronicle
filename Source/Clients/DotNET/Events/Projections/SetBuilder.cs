// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISetBuilder{TModel, TEvent, TProperty, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class SetBuilder<TModel, TEvent, TProperty, TParentBuilder> : ISetBuilder<TModel, TEvent, TProperty, TParentBuilder>
    where TParentBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TParentBuilder>
{
    readonly TParentBuilder _parent;
    readonly bool _forceEventProperty;
    string _expression = string.Empty;

    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

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
    public TParentBuilder To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor)
    {
        _expression = eventPropertyAccessor.GetPropertyPath();
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventSourceId()
    {
        ThrowIfOnlyEventPropertyIsSupported();

        _expression = "$eventSourceId";
        return _parent;
    }

    /// <inheritdoc/>
    public TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor)
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

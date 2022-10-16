// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of the <see cref="IJoinBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class JoinBuilder<TModel, TEvent> : ModelPropertiesBuilder<TModel, TEvent, IJoinBuilder<TModel, TEvent>>, IJoinBuilder<TModel, TEvent>
{
    PropertyPath? _on;

    /// <inheritdoc/>
    public IJoinBuilder<TModel, TEvent> On<TProperty>(Expression<Func<TModel, TProperty>> keyAccessor)
    {
        _on = keyAccessor.GetPropertyPath();
        return this;
    }

    /// <inheritdoc/>
    public JoinDefinition Build()
    {
        ThrowIfMissingOn();

        return new(
            on: _on!,
            Properties: _propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()),
            Key: _key,
            ParentKey: _parentKey);
    }

    void ThrowIfMissingOn()
    {
        if (_on is null)
        {
            throw new MissingOnPropertyExpressionWhenJoiningWithEvent(typeof(TModel), typeof(TEvent));
        }
    }
}

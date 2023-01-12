// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of the <see cref="IJoinBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
public class JoinBuilder<TModel, TEvent, TParentBuilder> : ModelPropertiesBuilder<TModel, TEvent, IJoinBuilder<TModel, TEvent>, TParentBuilder>, IJoinBuilder<TModel, TEvent>
    where TParentBuilder : class
{
    PropertyPath? _on;

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinBuilder{TModel, TEvent, TParentBuilder}"/>.
    /// </summary>
    /// <param name="projectionBuilder">The parent <see cref="IProjectionBuilder{TModel, TParentBuilder}"/>.</param>
    public JoinBuilder(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder) : base(projectionBuilder)
    {
    }

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
            On: _on!,
            Properties: _propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()),
            Key: _key.Build());
    }

    void ThrowIfMissingOn()
    {
        if (_on is null)
        {
            throw new MissingOnPropertyExpressionWhenJoiningWithEvent(typeof(TModel), typeof(TEvent));
        }
    }
}

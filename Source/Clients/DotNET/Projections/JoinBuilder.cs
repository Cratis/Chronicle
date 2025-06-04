// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of the <see cref="IJoinBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilder{TModel, TParentBuilder}"/>.</param>
public class JoinBuilder<TModel, TEvent, TParentBuilder>(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder)
    : ModelPropertiesBuilder<TModel, TEvent, IJoinBuilder<TModel, TEvent>, TParentBuilder>(projectionBuilder), IJoinBuilder<TModel, TEvent>
        where TParentBuilder : class
{
    readonly IProjectionBuilder<TModel, TParentBuilder> _projectionBuilder = projectionBuilder;
    PropertyPath? _on;

    /// <inheritdoc/>
    public IJoinBuilder<TModel, TEvent> On<TProperty>(Expression<Func<TModel, TProperty>> keyAccessor)
    {
        _on = keyAccessor.GetPropertyPath();
        return this;
    }

    /// <summary>
    /// Build <see cref="JoinDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="JoinDefinition"/>.</returns>
    internal JoinDefinition Build()
    {
        ThrowIfMissingOnForRootProjection();
        ThrowIfOnSpecifiedForChildProjection();
        ThrowIfMissingIdentifiedByForChildProjection();

        if (_on is null && _projectionBuilder is IChildrenBuilder childrenBuilder)
        {
            _on = childrenBuilder.GetIdentifiedBy();
        }

        return new()
        {
            On = _on!,
            Properties = _propertyExpressions.ToDictionary(_ => (string)_.Key, _ => _.Value.Build()),
            Key = _keyExpression
        };
    }

    void ThrowIfMissingOnForRootProjection()
    {
        if (_on is null && _projectionBuilder is not IChildrenBuilder)
        {
            throw new MissingOnPropertyExpressionWhenJoiningWithEvent(typeof(TModel), typeof(TEvent));
        }
    }

    void ThrowIfOnSpecifiedForChildProjection()
    {
        if (_on is not null && _projectionBuilder is IChildrenBuilder)
        {
            throw new OnPropertyShouldNotBeSpecifiedForChildJoin(typeof(TModel), typeof(TEvent));
        }
    }

    void ThrowIfMissingIdentifiedByForChildProjection()
    {
        if (_on is null && _projectionBuilder is IChildrenBuilder childrenBuilder && !childrenBuilder.HasIdentifiedBy)
        {
            throw new MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent(typeof(TModel), typeof(TEvent));
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of the <see cref="IJoinBuilder{TReadModel, TEvent}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilder{TReadModel, TParentBuilder}"/>.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
public class JoinBuilder<TReadModel, TEvent, TParentBuilder>(IProjectionBuilder<TReadModel, TParentBuilder> projectionBuilder, INamingPolicy namingPolicy)
    : ReadModelPropertiesBuilder<TReadModel, TEvent, IJoinBuilder<TReadModel, TEvent>, TParentBuilder>(projectionBuilder, namingPolicy), IJoinBuilder<TReadModel, TEvent>
        where TParentBuilder : class
{
    readonly IProjectionBuilder<TReadModel, TParentBuilder> _projectionBuilder = projectionBuilder;
    PropertyPath? _on;

    /// <inheritdoc/>
    public IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor)
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
            throw new MissingOnPropertyExpressionWhenJoiningWithEvent(typeof(TReadModel), typeof(TEvent));
        }
    }

    void ThrowIfOnSpecifiedForChildProjection()
    {
        if (_on is not null && _projectionBuilder is IChildrenBuilder)
        {
            throw new OnPropertyShouldNotBeSpecifiedForChildJoin(typeof(TReadModel), typeof(TEvent));
        }
    }

    void ThrowIfMissingIdentifiedByForChildProjection()
    {
        if (_on is null && _projectionBuilder is IChildrenBuilder childrenBuilder && !childrenBuilder.HasIdentifiedBy)
        {
            throw new MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent(typeof(TReadModel), typeof(TEvent));
        }
    }
}

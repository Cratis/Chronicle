// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationPropertyBuilder{TTarget, TSource}"/>
/// that converts expression-based property accessors to <see cref="PropertyName"/> values and
/// delegates to the underlying <see cref="IEventMigrationPropertyBuilder"/>.
/// </summary>
/// <typeparam name="TTarget">The target event type of the migration.</typeparam>
/// <typeparam name="TSource">The source event type of the migration.</typeparam>
/// <param name="inner">The underlying <see cref="IEventMigrationPropertyBuilder"/> to delegate to.</param>
public class EventMigrationPropertyBuilderFor<TTarget, TSource>(IEventMigrationPropertyBuilder inner) : IEventMigrationPropertyBuilder<TTarget, TSource>
{
    /// <inheritdoc/>
    public IEventMigrationPropertyBuilder<TTarget, TSource> Split<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        Expression<Func<TSource, object>> sourceProperty,
        PropertySeparator separator,
        SplitPartIndex part)
    {
        inner.Split(
            GetPropertyName(targetProperty),
            GetPropertyName(sourceProperty),
            separator,
            part);

        return this;
    }

    /// <inheritdoc/>
    public IEventMigrationPropertyBuilder<TTarget, TSource> Combine<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        PropertySeparator separator,
        params Expression<Func<TSource, object>>[] sourceProperties)
    {
        inner.Combine(
            GetPropertyName(targetProperty),
            separator,
            sourceProperties.Select(GetPropertyName).ToArray());

        return this;
    }

    /// <inheritdoc/>
    public IEventMigrationPropertyBuilder<TTarget, TSource> RenamedFrom<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        Expression<Func<TSource, object>> sourceProperty)
    {
        inner.RenamedFrom(
            GetPropertyName(targetProperty),
            GetPropertyName(sourceProperty));

        return this;
    }

    /// <inheritdoc/>
    public IEventMigrationPropertyBuilder<TTarget, TSource> DefaultValue<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        TProperty value)
    {
        inner.DefaultValue(
            GetPropertyName(targetProperty),
            value!);

        return this;
    }

    static PropertyName GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression) =>
        new(expression.GetPropertyPath());
}

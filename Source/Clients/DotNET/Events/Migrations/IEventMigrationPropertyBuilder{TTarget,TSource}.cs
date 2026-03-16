// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a type-safe builder for event migration property transformations using expressions.
/// </summary>
/// <typeparam name="TTarget">The target event type of the migration.</typeparam>
/// <typeparam name="TSource">The source event type of the migration.</typeparam>
public interface IEventMigrationPropertyBuilder<TTarget, TSource>
{
    /// <summary>
    /// Split a source property value into a target property by extracting one part.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target property.</typeparam>
    /// <param name="targetProperty">Expression selecting the target property to write the split result into.</param>
    /// <param name="sourceProperty">Expression selecting the source property to split.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="part">The zero-based part index to extract.</param>
    /// <returns>The builder for continued configuration.</returns>
    IEventMigrationPropertyBuilder<TTarget, TSource> Split<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        Expression<Func<TSource, object>> sourceProperty,
        PropertySeparator separator,
        SplitPartIndex part);

    /// <summary>
    /// Combine multiple source properties into a single target property by concatenation.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target property.</typeparam>
    /// <param name="targetProperty">Expression selecting the target property to write the combined result into.</param>
    /// <param name="sourceProperties">Expressions selecting the source properties to concatenate.</param>
    /// <returns>The builder for continued configuration.</returns>
    IEventMigrationPropertyBuilder<TTarget, TSource> Combine<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        params Expression<Func<TSource, object>>[] sourceProperties);

    /// <summary>
    /// Rename a property from an old name to a new target property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target property.</typeparam>
    /// <param name="targetProperty">Expression selecting the new target property.</param>
    /// <param name="sourceProperty">Expression selecting the old source property to read from.</param>
    /// <returns>The builder for continued configuration.</returns>
    IEventMigrationPropertyBuilder<TTarget, TSource> RenamedFrom<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        Expression<Func<TSource, object>> sourceProperty);

    /// <summary>
    /// Provide a default value for a new property that did not exist in the source generation.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target property.</typeparam>
    /// <param name="targetProperty">Expression selecting the target property to set the default value on.</param>
    /// <param name="value">The default value.</param>
    /// <returns>The builder for continued configuration.</returns>
    IEventMigrationPropertyBuilder<TTarget, TSource> DefaultValue<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        TProperty value);
}

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
    /// <param name="separator">The separator to use between the combined values.</param>
    /// <param name="sourceProperties">Expressions selecting the source properties to concatenate.</param>
    /// <returns>The builder for continued configuration.</returns>
    IEventMigrationPropertyBuilder<TTarget, TSource> Combine<TProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        PropertySeparator separator,
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

    /// <summary>
    /// Translate the individual values of a property that mean something different in the target generation.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target property.</typeparam>
    /// <typeparam name="TSourceProperty">The type of the source property.</typeparam>
    /// <param name="targetProperty">Expression selecting the target property to write the translated value into.</param>
    /// <param name="sourceProperty">Expression selecting the source property to read the value from.</param>
    /// <param name="map">Action declaring which value becomes which.</param>
    /// <returns>The builder for continued configuration.</returns>
    /// <remarks>
    /// This states the translation for one direction only. To state it once and have both directions follow, override
    /// <c>MapValues</c> on <see cref="EventTypeMigration{TUpgrade, TPrevious}"/> instead.
    /// </remarks>
    IEventMigrationPropertyBuilder<TTarget, TSource> MapValues<TProperty, TSourceProperty>(
        Expression<Func<TTarget, TProperty>> targetProperty,
        Expression<Func<TSource, TSourceProperty>> sourceProperty,
        Action<IValueMapBuilder<TSourceProperty, TProperty>> map);
}

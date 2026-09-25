// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for event migration property transformations.
/// </summary>
/// <remarks>
/// Property arguments are serialized JSON paths and are preserved exactly as supplied, including casing and
/// explicit JSON property names. No naming policy is applied to these raw strings. For automatic name resolution,
/// use typed member accessors through <see cref="IEventMigrationPropertyBuilder{TTarget, TSource}"/> instead.
/// Separators, default values, and mapped values are literals and are not renamed.
/// </remarks>
public interface IEventMigrationPropertyBuilder
{
    /// <summary>
    /// Split a source property value into a target property by extracting one part.
    /// </summary>
    /// <param name="targetProperty">The JSON property path to write the split result into.</param>
    /// <param name="sourceProperty">The JSON source property path to split.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="part">The zero-based part index to extract.</param>
    void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part);

    /// <summary>
    /// Combine multiple source properties into a single target property by concatenation.
    /// </summary>
    /// <param name="targetProperty">The JSON property path to write the combined result into.</param>
    /// <param name="separator">The separator to use between the combined values.</param>
    /// <param name="sourceProperties">The JSON source property paths to concatenate.</param>
    void Combine(PropertyName targetProperty, PropertySeparator separator, params PropertyName[] sourceProperties);

    /// <summary>
    /// Rename a property from an old name to a new target property.
    /// </summary>
    /// <param name="targetProperty">The JSON target property path.</param>
    /// <param name="oldName">The JSON source property path to read from.</param>
    void RenamedFrom(PropertyName targetProperty, PropertyName oldName);

    /// <summary>
    /// Provide a default value for a new property that did not exist in the source generation.
    /// </summary>
    /// <param name="targetProperty">The JSON property path to set the default value on.</param>
    /// <param name="value">The default value.</param>
    void DefaultValue(PropertyName targetProperty, object value);

    /// <summary>
    /// Translate the individual values of a property that mean something different in the target generation.
    /// </summary>
    /// <param name="targetProperty">The JSON property path to write the translated value into.</param>
    /// <param name="sourceProperty">The JSON source property path to read the value from.</param>
    /// <param name="mappings">The values that change meaning, and what they become.</param>
    /// <remarks>
    /// A value no mapping mentions is carried across unchanged, and so is the value of a payload that does not carry
    /// the source property at all.
    /// </remarks>
    void MapValues(PropertyName targetProperty, PropertyName sourceProperty, IEnumerable<ValueMapping> mappings);
}

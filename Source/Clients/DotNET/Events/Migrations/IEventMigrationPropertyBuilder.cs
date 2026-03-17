// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for event migration property transformations.
/// </summary>
public interface IEventMigrationPropertyBuilder
{
    /// <summary>
    /// Split a source property value into a target property by extracting one part.
    /// </summary>
    /// <param name="targetProperty">The name of the property to write the split result into.</param>
    /// <param name="sourceProperty">The source property to split.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="part">The zero-based part index to extract.</param>
    void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part);

    /// <summary>
    /// Combine multiple source properties into a single target property by concatenation.
    /// </summary>
    /// <param name="targetProperty">The name of the property to write the combined result into.</param>
    /// <param name="separator">The separator to use between the combined values.</param>
    /// <param name="sourceProperties">The source properties to concatenate.</param>
    void Combine(PropertyName targetProperty, PropertySeparator separator, params PropertyName[] sourceProperties);

    /// <summary>
    /// Rename a property from an old name to a new target property.
    /// </summary>
    /// <param name="targetProperty">The new name for the property.</param>
    /// <param name="oldName">The old property name to read from.</param>
    void RenamedFrom(PropertyName targetProperty, PropertyName oldName);

    /// <summary>
    /// Provide a default value for a new property that did not exist in the source generation.
    /// </summary>
    /// <param name="targetProperty">The name of the property to set the default value on.</param>
    /// <param name="value">The default value.</param>
    void DefaultValue(PropertyName targetProperty, object value);
}

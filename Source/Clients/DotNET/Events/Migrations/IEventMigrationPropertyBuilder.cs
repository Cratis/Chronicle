// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for event migration property transformations.
/// </summary>
public interface IEventMigrationPropertyBuilder
{
    /// <summary>
    /// Split a property value into parts.
    /// </summary>
    /// <param name="sourceProperty">The source property to split.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="part">The part index to extract.</param>
    /// <returns>Property value expression.</returns>
    string Split(string sourceProperty, string separator, int part);

    /// <summary>
    /// Combine multiple properties into one.
    /// </summary>
    /// <param name="properties">The properties to combine.</param>
    /// <returns>Property value expression.</returns>
    string Combine(params string[] properties);

    /// <summary>
    /// Rename a property from an old name.
    /// </summary>
    /// <param name="oldName">The old property name.</param>
    /// <returns>Property value expression.</returns>
    string RenamedFrom(string oldName);

    /// <summary>
    /// Provide a default value for a property.
    /// </summary>
    /// <param name="value">The default value.</param>
    /// <returns>Property value expression.</returns>
    string DefaultValue(object value);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a property within a JSON schema.
/// </summary>
public interface IJsonSchemaProperty
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the format of the property.
    /// </summary>
    string? Format { get; }

    /// <summary>
    /// Gets extension data for the property.
    /// </summary>
    IDictionary<string, object?> ExtensionData { get; }

    /// <summary>
    /// Gets the actual schema for this property.
    /// </summary>
    IJsonSchemaDocument ActualSchema { get; }

    /// <summary>
    /// Determines if this property is nullable.
    /// </summary>
    bool IsNullable { get; }
}
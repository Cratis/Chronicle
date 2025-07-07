// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a JSON schema document that can be used for validation and metadata operations.
/// </summary>
public interface IJsonSchemaDocument
{
    /// <summary>
    /// Gets the JSON representation of the schema.
    /// </summary>
    JsonObject ToJsonObject();

    /// <summary>
    /// Gets the JSON string representation of the schema.
    /// </summary>
    string ToJson();

    /// <summary>
    /// Gets extension data for the schema.
    /// </summary>
    IDictionary<string, object?> ExtensionData { get; set; }

    /// <summary>
    /// Gets the properties defined in this schema.
    /// </summary>
    IDictionary<string, IJsonSchemaProperty> Properties { get; }

    /// <summary>
    /// Gets whether this schema has a reference to another schema.
    /// </summary>
    bool HasReference { get; }

    /// <summary>
    /// Gets the referenced schema if it exists.
    /// </summary>
    IJsonSchemaDocument? Reference { get; }

    /// <summary>
    /// Gets the actual properties including inherited ones.
    /// </summary>
    IDictionary<string, IJsonSchemaProperty> ActualProperties { get; }

    /// <summary>
    /// Gets all schemas that this schema inherits from (allOf).
    /// </summary>
    IEnumerable<IJsonSchemaDocument> AllOf { get; }

    /// <summary>
    /// Gets the inherited schema if it exists.
    /// </summary>
    IJsonSchemaDocument? InheritedSchema { get; }
}
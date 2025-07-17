// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Implementation of <see cref="IJsonSchemaProperty"/> that wraps a .NET 9 JsonObject property schema.
/// </summary>
public class DotNet9JsonSchemaProperty : IJsonSchemaProperty
{
    readonly JsonObject _propertySchema;
    readonly Dictionary<string, object?> _extensionData;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNet9JsonSchemaProperty"/> class.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="propertySchema">The JsonObject representing the property schema.</param>
    public DotNet9JsonSchemaProperty(string name, JsonObject propertySchema)
    {
        Name = name;
        _propertySchema = propertySchema;
        _extensionData = new Dictionary<string, object?>();
        ActualSchema = new DotNet9JsonSchemaDocument(propertySchema);

        ParsePropertySchema();
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Format { get; private set; }

    /// <inheritdoc/>
    public IDictionary<string, object?> ExtensionData => _extensionData;

    /// <inheritdoc/>
    public IJsonSchemaDocument ActualSchema { get; }

    /// <inheritdoc/>
    public bool IsNullable { get; private set; }

    void ParsePropertySchema()
    {
        // Extract format
        if (_propertySchema["format"] is JsonValue formatValue)
        {
            Format = formatValue.ToString();
        }

        // Check if nullable - in .NET 9 schemas, this is represented as type array with "null"
        if (_propertySchema["type"] is JsonArray typeArray)
        {
            IsNullable = typeArray.Any(t => t?.ToString() == "null");
        }

        // Parse extension data
        foreach (var kvp in _propertySchema)
        {
            if (kvp.Key.StartsWith("x-"))
            {
                _extensionData[kvp.Key] = kvp.Value?.DeepClone();
            }
        }
    }
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Implementation of <see cref="IJsonSchemaDocument"/> that wraps a .NET 9 JsonObject schema.
/// </summary>
public class DotNet9JsonSchemaDocument : IJsonSchemaDocument
{
    readonly JsonObject _schema;
    readonly Dictionary<string, object?> _extensionData;
    readonly Dictionary<string, IJsonSchemaProperty> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNet9JsonSchemaDocument"/> class.
    /// </summary>
    /// <param name="schema">The JsonObject representing the schema.</param>
    public DotNet9JsonSchemaDocument(JsonObject schema)
    {
        _schema = schema;
        _extensionData = new Dictionary<string, object?>();
        _properties = new Dictionary<string, IJsonSchemaProperty>();

        ParseSchema();
    }

    /// <inheritdoc/>
    public JsonObject ToJsonObject() => _schema;

    /// <inheritdoc/>
    public string ToJson() => _schema.ToString();

    /// <inheritdoc/>
    public IDictionary<string, object?> ExtensionData => _extensionData;

    /// <inheritdoc/>
    public IDictionary<string, IJsonSchemaProperty> Properties => _properties;

    /// <inheritdoc/>
    public bool HasReference => false; // .NET 9 schemas don't use references in the same way

    /// <inheritdoc/>
    public IJsonSchemaDocument? Reference => null;

    /// <inheritdoc/>
    public IDictionary<string, IJsonSchemaProperty> ActualProperties => Properties;

    /// <inheritdoc/>
    public IEnumerable<IJsonSchemaDocument> AllOf => Array.Empty<IJsonSchemaDocument>();

    /// <inheritdoc/>
    public IJsonSchemaDocument? InheritedSchema => null;

    void ParseSchema()
    {
        if (_schema["properties"] is JsonObject properties)
        {
            foreach (var property in properties)
            {
                if (property.Value is JsonObject propertySchema)
                {
                    _properties[property.Key] = new DotNet9JsonSchemaProperty(property.Key, propertySchema);
                }
            }
        }

        // Parse extension data - look for custom properties that start with 'x-'
        foreach (var kvp in _schema)
        {
            if (kvp.Key.StartsWith("x-"))
            {
                _extensionData[kvp.Key] = kvp.Value?.DeepClone();
            }
        }
    }
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.DependencyInjection;
using Cratis.Json;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
[Singleton]
public partial class ExpandoObjectConverter(ITypeFormats typeFormats) : IExpandoObjectConverter
{
    readonly JsonSerializerOptions? _jsonSerializerOptions;
    readonly StringConversionKinds _stringKinds = new(typeFormats);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class with provider-specific serializer options.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
    /// <param name="jsonSerializerOptions">The options to use for formatted values.</param>
    public ExpandoObjectConverter(ITypeFormats typeFormats, JsonSerializerOptions jsonSerializerOptions)
        : this(typeFormats)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    JsonSerializerOptions SerializerOptions => _jsonSerializerOptions ?? Globals.JsonSerializerOptions;

    /// <inheritdoc/>
    public JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        var jsonObject = new JsonObject();
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        var schemaProperties = schema.FlattenedProperties;

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the expando object is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (key, value) in expandoObjectAsDictionary)
            {
                var node = UnknownValueConverter.ToJsonNode(value);
                if (node is not null)
                {
                    jsonObject[key] = node;
                }
            }

            return jsonObject;
        }

        foreach (var property in schemaProperties)
        {
            var node = ConvertToJsonNodeOrDefault(FindSourceValue(expandoObjectAsDictionary, property.Name), property);
            if (node is not null)
            {
                jsonObject[property.Name] = node;
            }
        }

        return jsonObject;
    }

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        var schemaProperties = schema.FlattenedProperties;

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the document is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (name, sourceValue) in document)
            {
                if (sourceValue is not null)
                {
                    expandoObjectAsDictionary[name] = UnknownValueConverter.ToClrType(sourceValue);
                }
            }

            return expandoObject;
        }

        foreach (var property in schemaProperties)
        {
            var sourceValue = FindSourceNode(document, property.Name);
            var value = sourceValue is not null ? ConvertFromJsonNode(sourceValue, property) : null;
            value ??= property.GetDefaultValue(typeFormats);
            if (value is not null)
            {
                expandoObjectAsDictionary[property.Name] = value;
            }
        }

        return expandoObject;
    }

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter writer, ExpandoObject expandoObject, JsonSchema schema) => WriteObject(writer, expandoObject, schema);

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(ReadOnlyMemory<byte> utf8Json, JsonSchema schema)
    {
        var reader = new Utf8JsonReader(utf8Json.Span);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("The JSON of a read model state must be an object.");
        }

        return ReadObject(ref reader, schema);
    }

    /// <summary>
    /// Finds the value of a property, preferring an exact-case match and falling back to a case-insensitive match.
    /// The source expando may carry both casings of the same name (e.g. both <c language="csharp">Id</c> and <c language="csharp">id</c>).
    /// </summary>
    /// <param name="source">The source as a dictionary.</param>
    /// <param name="name">The name of the property.</param>
    /// <returns>The value, or null when there is none.</returns>
    static object? FindSourceValue(IDictionary<string, object?> source, string name)
    {
        if (source.TryGetValue(name, out var sourceValue))
        {
            return sourceValue;
        }

        foreach (var candidate in source)
        {
            if (candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value;
            }
        }

        return null;
    }

    static JsonNode? FindSourceNode(JsonObject document, string name)
    {
        var sourceValue = document[name];
        if (sourceValue is not null)
        {
            return sourceValue;
        }

        foreach (var candidate in document)
        {
            if (candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value;
            }
        }

        return null;
    }

    JsonNode? ConvertToJsonNodeOrDefault(object? sourceValue, JsonSchemaProperty property)
    {
        var node = ConvertToJsonNode(sourceValue, property);
        if (node is not null)
        {
            return node;
        }

        var defaultValue = property.GetDefaultValue(typeFormats);
        return defaultValue is not null ? ConvertToJsonNode(defaultValue, property) : null;
    }
}

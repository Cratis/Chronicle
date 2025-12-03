// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Cratis.Chronicle.Compliance;
using Cratis.Serialization;
using Cratis.Strings;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IJsonSchemaGenerator"/> using .NET 9 JsonSchemaExporter.
/// </summary>
[Singleton]
public class JsonSchemaGenerator : IJsonSchemaGenerator
{
    readonly IComplianceMetadataResolver _metadataResolver;
    readonly TypeFormats _typeFormats;
    readonly JsonSerializerOptions _serializerOptions;
    readonly INamingPolicy _namingPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
    /// </summary>
    /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver, INamingPolicy namingPolicy)
    {
        _metadataResolver = metadataResolver;
        _typeFormats = new TypeFormats();
        _namingPolicy = namingPolicy;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = namingPolicy.JsonPropertyNamingPolicy,
            TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver
        };
    }

    /// <inheritdoc/>
    public IJsonSchemaDocument Generate(Type type)
    {
        var jsonNode = JsonSchemaExporter.GetJsonSchemaAsNode(_serializerOptions, type);
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new InvalidOperationException($"Expected JsonObject for schema generation, got {jsonNode?.GetType()}");
        }

        // Convert property names to match naming policy
        ConvertPropertyNames(jsonObject);

        // Apply compliance metadata processing
        ApplyComplianceMetadata(jsonObject, type);

        // Apply type format processing
        ApplyTypeFormats(jsonObject, type);

        return new DotNet9JsonSchemaDocument(jsonObject);
    }

    void ConvertPropertyNames(JsonObject schema)
    {
        if (schema["properties"] is JsonObject properties)
        {
            var convertedProperties = new JsonObject();
            foreach (var property in properties)
            {
                var convertedName = _namingPolicy.GetPropertyName(property.Key);
                convertedProperties[convertedName] = property.Value?.DeepClone();
            }
            schema["properties"] = convertedProperties;
        }
    }

    void ApplyComplianceMetadata(JsonObject schema, Type type)
    {
        // Add type-level compliance metadata
        if (_metadataResolver.HasMetadataFor(type))
        {
            var metadata = _metadataResolver.GetMetadataFor(type);
            AddComplianceMetadataToSchema(schema, metadata);
        }

        // Add property-level compliance metadata
        if (schema["properties"] is JsonObject properties)
        {
            foreach (var propertyKvp in properties)
            {
                var propertyName = propertyKvp.Key.ToPascalCase();
                var clrProperty = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                if (clrProperty != null && _metadataResolver.HasMetadataFor(clrProperty))
                {
                    var metadata = _metadataResolver.GetMetadataFor(clrProperty);
                    if (propertyKvp.Value is JsonObject propertySchema)
                    {
                        AddComplianceMetadataToSchema(propertySchema, metadata);
                    }
                }
            }
        }
    }

    void ApplyTypeFormats(JsonObject schema, Type type)
    {
        // Apply type format to root schema
        if (_typeFormats.IsKnown(type))
        {
            var format = _typeFormats.GetFormatForType(type);
            schema["format"] = format;
        }

        // Apply type formats to properties
        if (schema["properties"] is JsonObject properties)
        {
            foreach (var propertyKvp in properties)
            {
                var propertyName = propertyKvp.Key.ToPascalCase();
                var clrProperty = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                if (clrProperty != null && propertyKvp.Value is JsonObject propertySchema)
                {
                    var propertyType = clrProperty.PropertyType;

                    // Handle nullable types
                    var wasNullable = false;
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType)!;
                        wasNullable = true;
                    }

                    // Handle enum types - they are represented as integers
                    if (propertyType.IsEnum)
                    {
                        propertyType = Enum.GetUnderlyingType(propertyType);
                    }

                    if (_typeFormats.IsKnown(propertyType))
                    {
                        var format = _typeFormats.GetFormatForType(propertyType);

                        // Add nullable marker if the property is nullable
                        if (wasNullable)
                        {
                            format = $"{format}?";
                        }

                        propertySchema["format"] = format;
                    }
                }
            }
        }
    }

    void AddComplianceMetadataToSchema(JsonObject schema, IEnumerable<ComplianceMetadata> metadata)
    {
        var complianceList = new JsonArray();

        foreach (var item in metadata)
        {
            var complianceItem = new JsonObject
            {
                ["metadataType"] = item.MetadataType.Value.ToString(),
                ["details"] = item.Details
            };
            complianceList.Add(complianceItem);
        }

        if (complianceList.Count > 0)
        {
            schema["x-compliance"] = complianceList;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Compliance;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
    /// </summary>
    /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
    public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver)
    {
        _metadataResolver = metadataResolver;
        _typeFormats = new TypeFormats();

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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

        // Apply compliance metadata processing
        ApplyComplianceMetadata(jsonObject, type);

        // Apply type format processing
        ApplyTypeFormats(jsonObject, type);

        return new DotNet9JsonSchemaDocument(jsonObject);
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
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType)!;
                    }

                    if (_typeFormats.IsKnown(propertyType))
                    {
                        var format = _typeFormats.GetFormatForType(propertyType);

                        // Add nullable marker if the property is nullable
                        if (clrProperty.PropertyType.IsGenericType &&
                            clrProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
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

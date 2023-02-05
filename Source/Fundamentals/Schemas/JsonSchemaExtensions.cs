// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    const string FlattenedProperties = "x-flattened-properties";

    /// <summary>
    /// Ensure the compliance metadata is correct with correct types.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    public static void EnsureComplianceMetadata(this JsonSchema schema)
    {
        if ((schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false) &&
            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] is object[] complianceObjects)
        {
            var metadata = new List<ComplianceSchemaMetadata>();
            foreach (var complianceObject in complianceObjects)
            {
                if (complianceObject is Dictionary<object, object> properties)
                {
                    var metadataType = properties.FirstOrDefault(kvp => (kvp.Key as string) == nameof(ComplianceSchemaMetadata.metadataType));
                    var details = properties.FirstOrDefault(kvp => (kvp.Key as string) == nameof(ComplianceSchemaMetadata.details));
                    metadata.Add(new ComplianceSchemaMetadata(Guid.Parse(metadataType.Value.ToString()!), details.Value.ToString()!));
                }
            }

            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] = metadata;
        }

        if (schema.Properties != default)
        {
            foreach (var property in schema.Properties)
            {
                property.Value.EnsureComplianceMetadata();
            }
        }
    }

    /// <summary>
    /// Get compliance metadata from schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchema schema)
    {
        if ((schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false) &&
            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] is IEnumerable<ComplianceSchemaMetadata> allMetadata)
        {
            return allMetadata;
        }

        return Array.Empty<ComplianceSchemaMetadata>();
    }

    /// <summary>
    /// Check recursively if the schema has compliance metadata.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasComplianceMetadata(this JsonSchema schema)
    {
        var hasMetadata = schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false;

        if (!hasMetadata && schema.Properties != default)
        {
            foreach (var property in schema.GetFlattenedProperties())
            {
                hasMetadata = property.HasComplianceMetadata();
                if (hasMetadata) break;
            }
        }

        return hasMetadata;
    }

    /// <summary>
    /// Ensure the flattened properties are available.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure for.</param>
    public static void EnsureFlattenedProperties(this JsonSchema schema)
    {
        if (schema.ExtensionData?.ContainsKey(FlattenedProperties) != true || schema.ExtensionData?[FlattenedProperties] is not IEnumerable<JsonSchemaProperty>)
        {
            var properties = new List<JsonSchemaProperty>();
            CollectPropertiesFrom(schema, properties);

            schema.ExtensionData ??= new Dictionary<string, object>();
            schema.ExtensionData[FlattenedProperties] = properties.DistinctBy(_ => _.Name).ToArray().AsEnumerable();
        }
    }

    /// <summary>
    /// Reset the flattened properties.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to reset for.</param>
    public static void ResetFlattenedProperties(this JsonSchema schema)
    {
        schema.ExtensionData.Remove(FlattenedProperties);
    }

    /// <summary>
    /// Get all actual properties from a schema, including any inherited properties from inherited schemas.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="JsonSchemaProperty"/>.</returns>
    public static IEnumerable<JsonSchemaProperty> GetFlattenedProperties(this JsonSchema schema)
    {
        EnsureFlattenedProperties(schema);
        return (schema.ExtensionData[FlattenedProperties] as IEnumerable<JsonSchemaProperty>)!;
    }

    /// <summary>
    /// Gets the schema for a property within the schema hierarchy based on a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The actual <see cref="JsonSchema"/>.</returns>
    public static JsonSchema GetSchemaForPropertyPath(this JsonSchema schema, PropertyPath propertyPath)
    {
        foreach (var segment in propertyPath.Segments)
        {
            var properties = schema.GetFlattenedProperties();
            var schemaProperty = properties.SingleOrDefault(_ => _.Name == segment.Value);
            if (schemaProperty is not null)
            {
                if (schemaProperty.IsArray)
                {
                    schema = schemaProperty.Item.Reference;
                }
                else
                {
                    schema = schemaProperty.ActualTypeSchema;
                }
            }
        }

        return schema;
    }

    /// <summary>
    /// Gets the <see cref="JsonSchemaProperty"/> within the schema hierarchy based on a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The actual <see cref="JsonSchemaProperty"/>.</returns>
    public static JsonSchemaProperty? GetSchemaPropertyForPropertyPath(this JsonSchema schema, PropertyPath propertyPath)
    {
        var segments = propertyPath.Segments.ToArray();
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var properties = schema.GetFlattenedProperties();
            var segment = segments[segmentIndex];

            var schemaProperty = properties.SingleOrDefault(_ => _.Name == segment.Value);
            if (schemaProperty is not null)
            {
                if (segmentIndex == segments.Length - 1)
                {
                    return schemaProperty;
                }

                if (schemaProperty.IsArray)
                {
                    schema = schemaProperty.Item.Reference;
                }
                else
                {
                    schema = schemaProperty.ActualTypeSchema;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the type for a <see cref="PropertyPath"/> resolved through the <see cref="JsonSchema"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to find property from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The actual type or null if its not a known property path within the schema.</returns>
    public static Type? GetTargetTypeForPropertyPath(this JsonSchema schema, PropertyPath propertyPath, ITypeFormats typeFormats)
    {
        var schemaProperty = schema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (schemaProperty is not null)
        {
            return schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);
        }

        return null;
    }

    /// <summary>
    /// Gets the type for a <see cref="PropertyPath"/> resolved through the <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to find property from.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The actual type or null if its not a known property path within the schema.</returns>
    public static Type? GetTargetTypeForJsonSchemaProperty(this JsonSchemaProperty schemaProperty, ITypeFormats typeFormats)
    {
        if (typeFormats.IsKnown(schemaProperty.Format))
        {
            return typeFormats.GetTypeForFormat(schemaProperty.Format);
        }

        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                    schemaProperty.Reference?.Type ??
                        (schemaProperty.HasOneOfSchemaReference ?
                            schemaProperty.OneOf.First().Reference.Type :
                            JsonObjectType.None) :
                    schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        return type switch
        {
            JsonObjectType.String => typeof(string),
            JsonObjectType.Boolean => typeof(bool),
            JsonObjectType.Integer => typeof(int),
            JsonObjectType.Number => typeof(double),
            _ => null
        };
    }

    static void CollectPropertiesFrom(JsonSchema schema, List<JsonSchemaProperty> properties)
    {
        properties.AddRange(schema.ActualProperties.Select(_ => _.Value));
        if (schema.InheritedSchema is not null)
        {
            CollectPropertiesFrom(schema.InheritedSchema, properties);
        }
    }
}

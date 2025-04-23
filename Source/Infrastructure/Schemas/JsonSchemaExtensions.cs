// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    const string FlattenedProperties = "x-flattened-properties";

    /// <summary>
    /// Ensure the flattened properties are available.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure for.</param>
    public static void EnsureFlattenedProperties(this JsonSchema schema)
    {
        lock (schema)
        {
            if (schema.ExtensionData?.ContainsKey(FlattenedProperties) != true || schema.ExtensionData?[FlattenedProperties] is not IEnumerable<JsonSchemaProperty>)
            {
                var properties = new List<JsonSchemaProperty>();
                CollectPropertiesFrom(schema, properties);

                schema.ExtensionData ??= new Dictionary<string, object?>();
                schema.ExtensionData[FlattenedProperties] = properties.DistinctBy(_ => _.Name).ToArray().AsEnumerable();
            }
        }
    }

    /// <summary>
    /// Reset the flattened properties.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to reset for.</param>
    public static void ResetFlattenedProperties(this JsonSchema schema)
    {
        schema.ExtensionData?.Remove(FlattenedProperties);
    }

    /// <summary>
    /// Get all actual properties from a schema, including any inherited properties from inherited schemas.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="JsonSchemaProperty"/>.</returns>
    public static IEnumerable<JsonSchemaProperty> GetFlattenedProperties(this JsonSchema schema)
    {
        lock (schema)
        {
            EnsureFlattenedProperties(schema);
            return (schema.ExtensionData![FlattenedProperties] as IEnumerable<JsonSchemaProperty>)!;
        }
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
            var properties = schema!.GetFlattenedProperties();
            var schemaProperty = properties.SingleOrDefault(_ => _.Name == segment.Value);
            if (schemaProperty is not null)
            {
                if (schemaProperty.IsArray)
                {
                    schema = schemaProperty.Item!.Reference!;
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
            var properties = schema!.GetFlattenedProperties();
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
                    schema = schemaProperty.Item!.Reference!;
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
        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            return typeFormats.GetTypeForFormat(schemaProperty.Format!);
        }

        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                    schemaProperty.Reference?.Type ??
                        (schemaProperty.HasOneOfSchemaReference ?
                            schemaProperty.OneOf.First().Reference!.Type :
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

    /// <summary>
    /// Get the default value for a <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to get default value for.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The default value.</returns>
    public static object? GetDefaultValue(this JsonSchemaProperty schemaProperty, ITypeFormats typeFormats)
    {
        if (schemaProperty.IsNullable())
        {
            return null;
        }

        var type = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);

        if (type is not null && (type.IsPrimitive || !type.IsByRef) &&
                type != typeof(string) &&
                type != typeof(object))
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Get whether or not property format is set to nullable.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsNullable(this JsonSchemaProperty schemaProperty) => schemaProperty.Format?.EndsWith('?') ?? false;

    static void CollectPropertiesFrom(JsonSchema schema, List<JsonSchemaProperty> properties)
    {
        properties.AddRange(schema.ActualProperties.Select(_ => _.Value));
        if (schema.InheritedSchema is not null)
        {
            CollectPropertiesFrom(schema.InheritedSchema, properties);
        }
    }
}

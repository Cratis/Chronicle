// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="IJsonSchemaDocument"/>.
/// </summary>
public static class JsonSchemaDocumentExtensions
{
    const string FlattenedProperties = "x-flattened-properties";

    /// <summary>
    /// Ensure the flattened properties are available.
    /// </summary>
    /// <param name="schema"><see cref="IJsonSchemaDocument"/> to ensure for.</param>
    public static void EnsureFlattenedProperties(this IJsonSchemaDocument schema)
    {
        if (schema.ExtensionData?.ContainsKey(FlattenedProperties) != true || schema.ExtensionData?[FlattenedProperties] is not IEnumerable<IJsonSchemaProperty>)
        {
            var properties = new List<IJsonSchemaProperty>();
            CollectPropertiesFrom(schema, properties);

            schema.ExtensionData ??= new Dictionary<string, object?>();
            schema.ExtensionData[FlattenedProperties] = properties.DistinctBy(_ => _.Name).ToArray().AsEnumerable();
        }
    }

    /// <summary>
    /// Reset the flattened properties.
    /// </summary>
    /// <param name="schema"><see cref="IJsonSchemaDocument"/> to reset for.</param>
    public static void ResetFlattenedProperties(this IJsonSchemaDocument schema)
    {
        schema.ExtensionData?.Remove(FlattenedProperties);
    }

    /// <summary>
    /// Get all actual properties from a schema, including any inherited properties from inherited schemas.
    /// </summary>
    /// <param name="schema"><see cref="IJsonSchemaDocument"/> to get from.</param>
    /// <returns>Collection of <see cref="IJsonSchemaProperty"/>.</returns>
    public static IEnumerable<IJsonSchemaProperty> GetFlattenedProperties(this IJsonSchemaDocument schema)
    {
        EnsureFlattenedProperties(schema);
        return (IEnumerable<IJsonSchemaProperty>)schema.ExtensionData![FlattenedProperties]!;
    }

    /// <summary>
    /// Get the property schema for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="IJsonSchemaDocument"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns><see cref="IJsonSchemaProperty"/> for the path or null if not found.</returns>
    public static IJsonSchemaProperty? GetSchemaPropertyForPropertyPath(this IJsonSchemaDocument schema, PropertyPath propertyPath)
    {
        var properties = schema.GetFlattenedProperties();
        foreach (var property in properties)
        {
            if (property.Name.Equals(propertyPath.Path, StringComparison.InvariantCultureIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Get target type for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="IJsonSchemaDocument"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving types.</param>
    /// <returns>Target <see cref="Type"/> for the property path or null if not found.</returns>
    public static Type? GetTargetTypeForPropertyPath(this IJsonSchemaDocument schema, PropertyPath propertyPath, ITypeFormats typeFormats)
    {
        var schemaProperty = schema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (schemaProperty is not null)
        {
            return schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);
        }

        return null;
    }

    static void CollectPropertiesFrom(IJsonSchemaDocument schema, List<IJsonSchemaProperty> properties)
    {
        properties.AddRange(schema.ActualProperties.Values);
        if (schema.InheritedSchema is not null)
        {
            CollectPropertiesFrom(schema.InheritedSchema, properties);
        }
    }
}
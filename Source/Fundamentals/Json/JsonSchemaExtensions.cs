// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Json;

/// <summary>
/// Extension methods for working with <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    /// <summary>
    /// Get all actual properties from a schema, including any inherited properties from inherited schemas.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="JsonSchemaProperty"/>.</returns>
    public static IEnumerable<JsonSchemaProperty> GetFlattenedProperties(this JsonSchema schema)
    {
        var properties = new List<JsonSchemaProperty>();
        CollectPropertiesFrom(schema, properties);
        return properties.DistinctBy(_ => _.Name).ToArray();
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
            if (schema.ActualProperties.ContainsKey(segment.Value))
            {
                var schemaProperty = schema.ActualProperties[segment.Value];
                if (schemaProperty.IsArray)
                {
                    schema = schema.ActualProperties[segment.Value].Item.Reference;
                }
                else
                {
                    schema = schemaProperty.ActualTypeSchema;
                }
            }
        }

        return schema;
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

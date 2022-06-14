// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    static void CollectPropertiesFrom(JsonSchema schema, List<JsonSchemaProperty> properties)
    {
        properties.AddRange(schema.ActualProperties.Select(_ => _.Value));
        if (schema.InheritedSchema is not null)
        {
            CollectPropertiesFrom(schema.InheritedSchema, properties);
        }
    }
}

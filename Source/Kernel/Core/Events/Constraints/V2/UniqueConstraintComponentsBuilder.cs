// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Extracts schema-typed components without sorting, deduplicating, or filling missing values.
/// </summary>
/// <param name="converter">The schema-aware scalar converter.</param>
internal class UniqueConstraintComponentsBuilder(IExpandoObjectConverter converter)
{
    /// <summary>
    /// Builds components in the constraint's declared property order.
    /// </summary>
    /// <param name="definition">The event constraint definition.</param>
    /// <param name="content">The unprotected event content.</param>
    /// <param name="schema">The schema of that event generation.</param>
    /// <returns>One component for every declared property, including repeated and missing properties.</returns>
    internal IReadOnlyList<UniqueConstraintComponent> Build(UniqueConstraintEventDefinition definition, JsonObject content, JsonSchema schema) =>
        definition.Properties.Select(property => Build(property, content, schema)).ToArray();

    UniqueConstraintComponent Build(string path, JsonObject content, JsonSchema schema)
    {
        var (value, propertySchema) = Resolve(path, content, schema);
        var actualSchema = EffectiveSchema(propertySchema);
        var scalarSchema = actualSchema?.IsArray == true ? EffectiveSchema(actualSchema.Item) : actualSchema;
        var format = scalarSchema?.Format;
        var converted = value is null ? null : Convert(value, actualSchema, schema);
        return new(converted, format, propertySchema is not null);
    }

    object? Convert(JsonNode value, JsonSchema? propertySchema, JsonSchema root)
    {
        // Convert only this present property: whole-event conversion creates defaults and touches unrelated ciphertext.
        var conversionSchema = new JsonSchema();
        conversionSchema.Properties["value"] = new JsonSchemaProperty(
            "value", (JsonObject)(propertySchema?.Node.DeepClone() ?? new JsonObject()), root);
        var source = new JsonObject { ["value"] = value.DeepClone() };
        var converted = (IDictionary<string, object?>)converter.ToExpandoObject(source, conversionSchema);
        return converted["value"];
    }

    static (JsonNode? Value, JsonSchema? Schema) Resolve(string path, JsonObject content, JsonSchema schema)
    {
        JsonNode? value = content;
        JsonSchema? currentSchema = schema;
        foreach (var segment in path.Split('.'))
        {
            value = (value as JsonObject)?.FirstOrDefault(property => property.Key.Equals(segment, StringComparison.OrdinalIgnoreCase)).Value;
            currentSchema = EffectiveSchema(currentSchema)?.GetFlattenedProperties()
                .FirstOrDefault(property => property.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
        }
        return (value, currentSchema);
    }

    static JsonSchema? EffectiveSchema(JsonSchema? schema)
    {
        var actual = schema?.ActualTypeSchema;
        return actual?.OneOf.FirstOrDefault(option => option.Type != JsonObjectType.Null)?.ActualTypeSchema ?? actual;
    }
}

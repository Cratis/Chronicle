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

    static (JsonNode? Value, JsonSchema? Schema) Resolve(string path, JsonObject content, JsonSchema schema)
    {
        var value = (JsonNode?)content;
        var currentSchema = (JsonSchema?)schema;
        foreach (var segment in path.Split('.'))
        {
            value = (value as JsonObject)?.FirstOrDefault(property => property.Key.Equals(segment, StringComparison.OrdinalIgnoreCase)).Value;
            currentSchema = ObjectProperties(currentSchema)
                .FirstOrDefault(property => property.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
        }
        return (value, currentSchema);
    }

    static IEnumerable<JsonSchemaProperty> ObjectProperties(JsonSchema? schema)
    {
        if (schema is null) return [];
        if (schema.Reference is { } reference) return ObjectProperties(reference);
        var nonNull = schema.OneOf.Concat(schema.AnyOf).FirstOrDefault(option =>
            option.Type != JsonObjectType.Null && option.Reference?.Type != JsonObjectType.Null);

        // Flatten the complete object composition, not ActualTypeSchema's single allOf branch.
        return nonNull is not null ? ObjectProperties(nonNull) : schema.GetFlattenedProperties();
    }

    static JsonSchema? EffectiveSchema(JsonSchema? schema)
    {
        var actual = schema?.ActualTypeSchema;
        return actual?.OneOf.FirstOrDefault(option => option.Type != JsonObjectType.Null)?.ActualTypeSchema ?? actual;
    }

    static Guid ReadGuid(JsonNode value) => value.AsValue().TryGetValue<Guid>(out var guid)
        ? guid
        : Guid.Parse(value.GetValue<string>());

    UniqueConstraintComponent Build(string path, JsonObject content, JsonSchema schema)
    {
        var (value, propertySchema) = Resolve(path, content, schema);
        var actualSchema = EffectiveSchema(propertySchema);
        var scalarSchema = actualSchema?.IsArray == true ? EffectiveSchema(actualSchema.Item) : actualSchema;
        var format = scalarSchema?.Format;
        if (format?.EndsWith('?') == true)
        {
            format = format[..^1];
        }
        var converted = value is null ? null : Convert(value, actualSchema, schema);
        return new(converted, format, propertySchema is not null);
    }

    object? Convert(JsonNode value, JsonSchema? propertySchema, JsonSchema root)
    {
        var scalarSchema = propertySchema?.IsArray == true ? EffectiveSchema(propertySchema.Item) : propertySchema;
        var format = scalarSchema?.Format?.TrimEnd('?');
        if (string.Equals(format, "guid", StringComparison.Ordinal) || string.Equals(format, "uuid", StringComparison.Ordinal))
        {
            // Persisted GUID strings need parsing, not JsonValue.GetValue<Guid>() CLR casting.
            return value is JsonArray array
                ? array.Select(item => item is null ? null : (object?)ReadGuid(item)).ToArray()
                : ReadGuid(value);
        }

        // Convert only this present property: whole-event conversion creates defaults and touches unrelated ciphertext.
        var conversionSchema = new JsonSchema();
        var conversionProperty = new JsonSchemaProperty(
            "value", propertySchema is null ? new JsonObject() : JsonNode.Parse(propertySchema.ToJson())!.AsObject(), root);
        if (propertySchema?.IsArray == true)
        {
            conversionProperty.Item = EffectiveSchema(propertySchema.Item);
        }
        conversionSchema.Properties["value"] = conversionProperty;
        var source = new JsonObject { ["value"] = value.DeepClone() };
        var converted = (IDictionary<string, object?>)converter.ToExpandoObject(source, conversionSchema);
        return converted["value"];
    }
}

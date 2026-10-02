// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Resolves declarations of the same value without discarding protection on references or duplicate members.
/// </summary>
public static class SchemaCompositionExtensions
{
    static readonly ConditionalWeakTable<JsonSchema, JsonSchema> _resolved = new();
    static readonly ConditionalWeakTable<JsonSchema, JsonSchema> _protectedValues = new();

    /// <summary>
    /// Resolves references and compositions for one value, retaining every member declaration.
    /// </summary>
    /// <param name="schema">The value schema.</param>
    /// <param name="protectsValue">Whether the containing declaration protects this entire value.</param>
    /// <returns>The resolved schema, with nested members resolved on demand.</returns>
    /// <exception cref="UnresolvedSchemaProtection">The declarations cannot be safely combined.</exception>
    public static JsonSchema ResolveComposition(this JsonSchema schema, bool protectsValue = false) => protectsValue
        ? _protectedValues.GetValue(schema, value => Resolve(value, true))
        : _resolved.GetValue(schema, value => Resolve(value, false));

    /// <summary>
    /// Determines whether union members may pass through without selecting a branch or bypassing protection.
    /// </summary>
    /// <param name="schema">The resolved container schema.</param>
    /// <returns>True when the union and container declare no protection on undeclared members.</returns>
    public static bool PreservesUnprotectedUnionMembers(this JsonSchema schema) =>
        (schema.AnyOf.Count > 0 || schema.OneOf.Count > 0) &&
        schema.IsUnprotectedSchemaValue(includeMembers: false) &&
        schema.AnyOf.Concat(schema.OneOf).All(alternative => alternative.IsUnprotectedSchemaValue());

    static JsonSchema Resolve(JsonSchema schema, bool protectsValue)
    {
        if (!schema.HasReference && schema.AllOf.Count == 0 && schema.AnyOf.Count == 0 && schema.OneOf.Count == 0)
        {
            return schema;
        }

        var node = new JsonObject();
        Merge(schema, node, [], protectsValue || !schema.IsUnprotectedSchemaValue(includeMembers: false));
        return new JsonSchema(node, schema.Root);
    }

    static void Merge(JsonSchema schema, JsonObject result, HashSet<(JsonSchema Root, string Reference)> references, bool protectsValue)
    {
        if (schema.Node["$ref"] is { } referenceNode)
        {
            var reference = referenceNode.GetValue<string>();
            var key = (schema.Root, reference);
            if (!references.Add(key) || schema.Reference is not { } target)
            {
                throw new UnresolvedSchemaProtection(reference);
            }
            Merge(target, result, references, protectsValue);
            references.Remove(key);
        }

        foreach (var declaration in schema.AllOf)
        {
            Merge(declaration, result, references, protectsValue);
        }

        foreach (var (keyword, alternatives) in new[] { ("anyOf", schema.AnyOf), ("oneOf", schema.OneOf) }.Where(_ => _.Item2.Count > 0))
        {
            var scalarAlternative = alternatives.Where(_ => _.ActualTypeSchema.Type != JsonObjectType.Null).ToArray();
            var isScalarUnion = scalarAlternative.Length == 1 && scalarAlternative[0].ActualTypeSchema.Type is JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean;
            if (!isScalarUnion && !protectsValue && alternatives.All(_ => _.IsUnprotectedSchemaValue()))
            {
                // A union without protection does not select one shape for conversion. Keep its existing
                // conversion behavior, including when a sibling property carries protection.
                result[keyword] = schema.Node[keyword]!.DeepClone();
                continue;
            }

            var resolvedAlternatives = alternatives.Select(candidate =>
            {
                var resolved = new JsonObject();
                Merge(candidate, resolved, references, protectsValue);
                return new JsonSchema(resolved, schema.Root);
            }).ToArray();
            var nonNull = resolvedAlternatives.Where(_ => _.Type != JsonObjectType.Null).ToArray();
            if (nonNull.Length != 1 || resolvedAlternatives.Any(_ => _.Type == JsonObjectType.Null && _.HasSchemaMetadata()))
            {
                throw new UnresolvedSchemaProtection("ambiguous union");
            }
            var alternative = new JsonObject();
            Merge(nonNull[0], alternative, references, protectsValue);
            if (resolvedAlternatives.Any(_ => _.Type == JsonObjectType.Null))
            {
                var nullable = new JsonSchema(alternative);
                nullable.Type |= JsonObjectType.Null;
            }
            Merge(new JsonSchema(alternative, schema.Root), result, references, protectsValue);
        }

        foreach (var (key, value) in schema.Node.Where(_ => _.Key is not "$ref" and not "allOf" and not "anyOf" and not "oneOf"))
        {
            if (key == "properties" && value is JsonObject properties)
            {
                var merged = result[key] as JsonObject ?? new JsonObject();
                if (result[key] is null) result[key] = merged;
                foreach (var (name, property) in properties)
                {
                    merged[name] = merged[name] is { } existing
                        ? Compose(existing, property!)
                        : property?.DeepClone();
                }
            }
            else if ((key == "compliance" || key == "security") && value is JsonArray metadata)
            {
                var combined = result[key] as JsonArray ?? new JsonArray();
                if (result[key] is null) result[key] = combined;
                foreach (var entry in metadata) combined.Add(entry?.DeepClone());
            }
            else if (key == "items" && result[key] is { } items)
            {
                result[key] = Compose(items, value!);
            }
            else if ((key == "type" || key == "format" || key == "enum" || key == "x-enumNames") && result[key] is { } previous && !JsonNode.DeepEquals(previous, value))
            {
                throw new UnresolvedSchemaProtection($"conflicting {key} declarations");
            }
            else
            {
                result[key] = value?.DeepClone();
            }
        }
    }

    static JsonObject Compose(JsonNode first, JsonNode second) => new()
    {
        ["allOf"] = new JsonArray(first.DeepClone(), second.DeepClone())
    };
}

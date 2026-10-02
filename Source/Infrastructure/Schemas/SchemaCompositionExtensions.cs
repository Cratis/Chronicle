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

    /// <summary>
    /// Resolves references and compositions for one value, retaining every member declaration.
    /// </summary>
    /// <param name="schema">The value schema.</param>
    /// <returns>The resolved schema, with nested members resolved on demand.</returns>
    /// <exception cref="UnresolvedSchemaProtection">The declarations cannot be safely combined.</exception>
    public static JsonSchema ResolveComposition(this JsonSchema schema) => _resolved.GetValue(schema, Resolve);

    static JsonSchema Resolve(JsonSchema schema)
    {
        if (!schema.HasReference && schema.AllOf.Count == 0 && schema.AnyOf.Count == 0 && schema.OneOf.Count == 0)
        {
            return schema;
        }

        var node = new JsonObject();
        Merge(schema, node, []);
        return new JsonSchema(node, schema.Root);
    }

    static void Merge(JsonSchema schema, JsonObject result, HashSet<(JsonSchema Root, string Reference)> references)
    {
        if (schema.Node["$ref"] is { } referenceNode)
        {
            var reference = referenceNode.GetValue<string>();
            var key = (schema.Root, reference);
            if (!references.Add(key) || schema.Reference is not { } target)
            {
                throw new UnresolvedSchemaProtection(reference);
            }
            Merge(target, result, references);
            references.Remove(key);
        }

        foreach (var declaration in schema.AllOf)
        {
            Merge(declaration, result, references);
        }

        foreach (var alternatives in new[] { schema.AnyOf, schema.OneOf }.Where(_ => _.Count > 0))
        {
            var nonNull = alternatives.Where(_ => _.Type != JsonObjectType.Null).ToArray();
            if (nonNull.Length != 1)
            {
                throw new UnresolvedSchemaProtection("ambiguous union");
            }
            Merge(nonNull[0], result, references);
            if (alternatives.Any(_ => _.Type == JsonObjectType.Null))
            {
                var nullable = new JsonSchema(result);
                nullable.Type |= JsonObjectType.Null;
            }
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

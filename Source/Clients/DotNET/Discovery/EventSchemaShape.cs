// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Extracts a canonical coarse shape from an event schema.
/// </summary>
internal static class EventSchemaShape
{
    /// <summary>
    /// Normalizes a nonempty event schema for identity comparison.
    /// </summary>
    /// <param name="schema">The JSON schema.</param>
    /// <returns>The canonical shape, or null when comparison is unsafe.</returns>
    internal static string? Of(string schema)
    {
        if (JsonNode.Parse(schema) is not JsonObject root)
        {
            return null;
        }

        var shape = Normalize(root, root, 0);

        return shape?["properties"] is JsonObject { Count: > 0 } ? shape.ToJsonString() : null;
    }

    static JsonObject? Normalize(JsonObject node, JsonObject root, int depth)
    {
        // Cyclic or unresolved references cannot establish an identical shape safely.
        if (depth > 64)
        {
            return null;
        }

        if (node["$ref"] is JsonValue reference)
        {
            var target = Resolve(reference.GetValue<string>(), root);
            return target is null ? null : Normalize(target, root, depth + 1);
        }

        var alternatives = node["anyOf"] as JsonArray ?? node["oneOf"] as JsonArray;
        if (alternatives is not null)
        {
            var variants = alternatives.OfType<JsonObject>().Where(_ => _["type"]?.ToJsonString() != "\"null\"")
                .Select(_ => Normalize(_, root, depth + 1)).ToArray();
            if (variants.Any(_ => _ is null) || variants.Length == 0)
            {
                return null;
            }

            return variants.Length == 1 ? variants[0] : new JsonObject
            {
                ["variants"] = new JsonArray(variants.Select(_ => _!.ToJsonString()).Distinct().Order(StringComparer.Ordinal).Select(_ => (JsonNode?)JsonValue.Create(_)).ToArray())
            };
        }

        var types = TypesOf(node).ToArray();
        var result = new JsonObject { ["type"] = string.Join('|', types) };
        if (node["properties"] is JsonObject properties)
        {
            var normalized = new JsonObject();
            foreach (var property in properties.OrderBy(_ => _.Key, StringComparer.Ordinal))
            {
                var shape = property.Value is JsonObject value ? Normalize(value, root, depth + 1) : null;
                if (shape is null)
                {
                    return null;
                }

                normalized[property.Key] = shape;
            }

            result["properties"] = normalized;
        }

        if (node["items"] is JsonObject items)
        {
            var shape = Normalize(items, root, depth + 1);
            if (shape is null)
            {
                return null;
            }

            result["items"] = shape;
        }

        if (node["additionalProperties"] is JsonObject additional)
        {
            var shape = Normalize(additional, root, depth + 1);
            if (shape is null)
            {
                return null;
            }

            result["additionalProperties"] = shape;
        }

        return result;
    }

    static IEnumerable<string> TypesOf(JsonObject node)
    {
        var types = node["type"] switch
        {
            JsonArray array => array.OfType<JsonValue>().Select(_ => _.GetValue<string>()),
            JsonValue value => [value.GetValue<string>()],
            _ => node.ContainsKey("properties") ? ["object"] : Array.Empty<string>()
        };

        return types.Where(_ => _ != "null").Select(_ => _ == "integer" ? "number" : _).Distinct().Order(StringComparer.Ordinal);
    }

    static JsonObject? Resolve(string reference, JsonObject root)
    {
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return null;
        }

        JsonNode? target = root;
        foreach (var segment in reference[2..].Split('/'))
        {
            target = (target as JsonObject)?[segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal)];
        }

        return target as JsonObject;
    }
}

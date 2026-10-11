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

        var shape = new Normalizer(root).Normalize(root, 0);

        return shape?["properties"] is JsonObject { Count: > 0 } ? shape.ToJsonString() : null;
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

    /// <summary>
    /// Normalizes schema nodes relative to one root schema.
    /// </summary>
    /// <param name="root">The root schema that references resolve against.</param>
    sealed class Normalizer(JsonObject root)
    {
        const int MaximumDepth = 64;

        /// <summary>
        /// Normalizes a schema node.
        /// </summary>
        /// <param name="node">The node to normalize.</param>
        /// <param name="depth">The current depth.</param>
        /// <returns>The normalized node, or null when comparison is unsafe.</returns>
        public JsonObject? Normalize(JsonObject node, int depth)
        {
            // Cyclic or unresolved references cannot establish an identical shape safely.
            if (depth > MaximumDepth)
            {
                return null;
            }

            if (node["$ref"] is JsonValue reference)
            {
                return NormalizeReference(reference.GetValue<string>(), depth);
            }

            if ((node["anyOf"] as JsonArray ?? node["oneOf"] as JsonArray) is JsonArray alternatives)
            {
                return NormalizeAlternatives(alternatives, depth);
            }

            return NormalizeNode(node, depth);
        }

        JsonObject? NormalizeReference(string reference, int depth)
        {
            var target = Resolve(reference, root);
            return target is null ? null : Normalize(target, depth + 1);
        }

        JsonObject? NormalizeNode(JsonObject node, int depth)
        {
            var result = new JsonObject { ["type"] = string.Join('|', TypesOf(node)) };
            string[] children = ["items", "additionalProperties"];
            var complete = TryAddProperties(node, result, depth) && children.All(_ => TryAddChild(node, _, result, depth));
            return complete ? result : null;
        }

        JsonObject? NormalizeAlternatives(JsonArray alternatives, int depth)
        {
            var variants = alternatives.OfType<JsonObject>()
                .Where(_ => _["type"]?.ToJsonString() != "\"null\"")
                .Select(_ => Normalize(_, depth + 1))
                .ToArray();
            if (variants.Length == 0 || variants.Any(_ => _ is null))
            {
                return null;
            }

            if (variants.Length == 1)
            {
                return variants[0];
            }

            var distinct = variants.Select(_ => _!.ToJsonString()).Distinct().Order(StringComparer.Ordinal);
            return new JsonObject { ["variants"] = new JsonArray(distinct.Select(_ => (JsonNode?)JsonValue.Create(_)).ToArray()) };
        }

        bool TryAddProperties(JsonObject node, JsonObject result, int depth)
        {
            if (node["properties"] is not JsonObject properties)
            {
                return true;
            }

            var normalized = new JsonObject();
            foreach (var property in properties.OrderBy(_ => _.Key, StringComparer.Ordinal))
            {
                var shape = property.Value is JsonObject value ? Normalize(value, depth + 1) : null;
                if (shape is null)
                {
                    return false;
                }

                normalized[property.Key] = shape;
            }

            result["properties"] = normalized;
            return true;
        }

        bool TryAddChild(JsonObject node, string key, JsonObject result, int depth)
        {
            if (node[key] is not JsonObject child)
            {
                return true;
            }

            var shape = Normalize(child, depth + 1);
            if (shape is null)
            {
                return false;
            }

            result[key] = shape;
            return true;
        }
    }
}

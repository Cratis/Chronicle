// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Walks schema declarations and reconciles default-only property refinements.
/// </summary>
internal static class DefaultOnlyPropertyRefinement
{
    static readonly string[] _metadataKeys = ["compliance", "security"];
    static readonly string[] _maps = ["properties", "$defs", "definitions", "patternProperties", "dependentSchemas"];
    static readonly string[] _children = ["items", "additionalProperties", "unevaluatedProperties", "contains", "propertyNames", "not", "if", "then", "else"];
    static readonly string[] _arrays = ["allOf", "anyOf", "oneOf", "prefixItems", "items"];
    static readonly string[] _refinementKeys = ["default", "title", "type", "format", "compliance", "security", "items", "enum", "x-enumNames", "$comment", "pattern", "minLength", "maxLength", "properties", "required", "additionalProperties", "oneOf", "anyOf", "$ref"];

    /// <summary>
    /// Walks matching schema declarations, excluding maps and arbitrary data.
    /// </summary>
    /// <param name="stored">The stored schema declaration.</param>
    /// <param name="generated">The incoming declaration.</param>
    /// <param name="property">Whether the declaration describes an object property.</param>
    /// <param name="path">The declaration's schema path.</param>
    /// <returns>The matching declarations.</returns>
    internal static IEnumerable<(JsonObject Stored, JsonObject Generated, bool Property, string Path)> Pairs(JsonNode? stored, JsonNode? generated, bool property = false, string path = "$")
    {
        if (stored is not JsonObject previous || generated is not JsonObject incoming)
        {
            yield break;
        }

        yield return (previous, incoming, property, path);
        foreach (var key in _maps)
        {
            if (previous[key] is not JsonObject oldMap || incoming[key] is not JsonObject newMap)
            {
                continue;
            }
            foreach (var entry in oldMap.Where(entry => newMap.ContainsKey(entry.Key)))
            {
                foreach (var pair in Pairs(entry.Value, newMap[entry.Key], key == "properties", $"{path}/{key}/{Escape(entry.Key)}"))
                {
                    yield return pair;
                }
            }
        }
        foreach (var key in _children)
        {
            foreach (var pair in Pairs(previous[key], incoming[key], false, $"{path}/{key}"))
            {
                yield return pair;
            }
        }
        foreach (var key in _arrays)
        {
            if (previous[key] is not JsonArray oldArray || incoming[key] is not JsonArray newArray)
            {
                continue;
            }
            for (var index = 0; index < Math.Min(oldArray.Count, newArray.Count); index++)
            {
                foreach (var pair in Pairs(oldArray[index], newArray[index], false, $"{path}/{key}/{index}"))
                {
                    yield return pair;
                }
            }
        }
    }

    /// <summary>
    /// Normalizes declarations without interpreting data as schema.
    /// </summary>
    /// <param name="node">The schema root.</param>
    /// <param name="normalize">The declaration transformation.</param>
    internal static void Normalize(JsonNode? node, Action<JsonObject> normalize)
    {
        // Pairing a schema with itself visits schema declarations only, never property maps or default payloads.
        foreach (var (schema, _, _, _) in Pairs(node, node).ToArray())
        {
            normalize(schema);
        }
    }

    /// <summary>
    /// Erases permitted refinements before comparing the remaining shape.
    /// </summary>
    /// <param name="stored">The stored schema root.</param>
    /// <param name="generated">The incoming schema root.</param>
    /// <returns>Whether every protection change was additive.</returns>
    internal static bool EraseCompatibleDifferences(JsonNode? stored, JsonNode? generated)
    {
        foreach (var (previous, incoming, property, _) in Pairs(stored, generated).ToArray())
        {
            foreach (var key in _metadataKeys)
            {
                if (!MetadataOnlyAdded(previous[key], incoming[key]))
                {
                    return false;
                }
                previous.Remove(key);
                incoming.Remove(key);
            }
            if (property && (IsRefinement(previous, incoming) || IsRefinement(incoming, previous)))
            {
                // The legacy declaration had no representation at all. Erase the newly supplied
                // representation, not constraints on declarations that were already typed.
                foreach (var key in _refinementKeys.Where(key => key != "default"))
                {
                    previous.Remove(key);
                    incoming.Remove(key);
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Finds declarations with newly added protection metadata.
    /// </summary>
    /// <param name="stored">The stored schema root.</param>
    /// <param name="generated">The incoming schema root.</param>
    /// <returns>The declaration paths.</returns>
    internal static IEnumerable<string> AddedMetadataPaths(JsonNode? stored, JsonNode? generated) =>
        Pairs(stored, generated).Where(pair => _metadataKeys.Any(key => pair.Generated[key] is JsonArray { Count: > 0 } added &&
            added.Any(item => pair.Stored[key] is not JsonArray previous || !previous.Any(old => JsonNode.DeepEquals(old, item)))))
            .Select(pair => pair.Path);

    /// <summary>
    /// Retains known type information when a legacy client submits a less precise shape.
    /// </summary>
    /// <param name="stored">The stored schema root.</param>
    /// <param name="generated">The incoming schema root to refine.</param>
    internal static void RetainPreciseProperties(JsonNode? stored, JsonNode? generated)
    {
        foreach (var (previous, incoming, property, _) in Pairs(stored, generated).ToArray())
        {
            if (!property || !IsRefinement(incoming, previous))
            {
                continue;
            }
            var metadata = _metadataKeys.Where(incoming.ContainsKey).ToDictionary(key => key, key => incoming[key]?.DeepClone());
            incoming.Clear();
            foreach (var entry in previous)
            {
                incoming[entry.Key] = entry.Value?.DeepClone();
            }
            foreach (var entry in metadata)
            {
                incoming[entry.Key] = entry.Value;
            }
        }
    }

    /// <summary>
    /// Checks that no declaration removed or changed protection metadata.
    /// </summary>
    /// <param name="stored">The stored metadata array.</param>
    /// <param name="generated">The incoming metadata array.</param>
    /// <returns>Whether all stored declarations remain.</returns>
    internal static bool MetadataOnlyAdded(JsonNode? stored, JsonNode? generated)
    {
        if (stored is not null and not JsonArray || generated is not null and not JsonArray)
        {
            return false;
        }
        return stored is not JsonArray previous || previous.All(item =>
            generated is JsonArray incoming && incoming.Any(next => JsonNode.DeepEquals(item, next)));
    }

    static string Escape(string value) => value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    static bool IsDefaultOnly(JsonObject node) => node.ContainsKey("default") &&
        node.All(entry => entry.Key == "default" || entry.Key == "title" || _metadataKeys.Contains(entry.Key));

    static bool IsRefinement(JsonObject untyped, JsonObject typed) => IsDefaultOnly(untyped) &&
        typed.ContainsKey("default") && typed.ContainsKey("type") &&
        JsonNode.DeepEquals(untyped["default"], typed["default"]) &&
        typed.All(entry => _refinementKeys.Contains(entry.Key));
}

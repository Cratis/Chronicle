// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    static readonly string[] _singleSchemaKeywords = ["not", "if", "then", "else", "contains", "propertyNames", "additionalProperties", "additionalItems", "unevaluatedProperties", "unevaluatedItems", "contentSchema"];
    static readonly string[] _schemaMapKeywords = ["properties", "patternProperties", "dependentSchemas", "$defs", "definitions", "dependencies"];
    static readonly string[] _schemaArrayKeywords = ["allOf", "anyOf", "oneOf", "prefixItems"];

    static bool IsBooleanSchema(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out _);

    static IEnumerable<(JsonNode? Node, bool Optional)> SchemaChildren(JsonObject node, bool includeDefinitions = true)
    {
        // Enumerate schema positions only: examples, defaults, const and metadata are instance data.
        var singles = _singleSchemaKeywords.Where(node.ContainsKey).Select(key => (node[key], false));
        var arrays = _schemaArrayKeywords.Where(node.ContainsKey).SelectMany(key => SchemaArrayChildren(node[key]));
        return singles.Concat(SchemaMapChildren(node, includeDefinitions)).Concat(arrays).Concat(SchemaItemChildren(node));
    }

    static IEnumerable<(JsonNode? Node, bool Optional)> SchemaMapChildren(JsonObject node, bool includeDefinitions)
    {
        foreach (var keyword in _schemaMapKeywords.Where(node.ContainsKey))
        {
            if (!includeDefinitions && IsDefinitionKeyword(keyword)) continue;
            foreach (var entry in SchemaMapEntries(node[keyword], keyword == "dependencies")) yield return entry;
        }
    }

    static IEnumerable<(JsonNode? Node, bool Optional)> SchemaMapEntries(JsonNode? node, bool propertyDependencies)
    {
        if (node is not JsonObject map)
        {
            yield return (null, false);
            yield break;
        }
        foreach (var entry in map) yield return (entry.Value, propertyDependencies && entry.Value is JsonArray);
    }

    static IEnumerable<(JsonNode? Node, bool Optional)> SchemaArrayChildren(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            yield return (null, false);
            yield break;
        }
        foreach (var child in array) yield return (child, false);
    }

    static IEnumerable<(JsonNode? Node, bool Optional)> SchemaItemChildren(JsonObject node)
    {
        if (!node.ContainsKey("items")) return [];
        return node["items"] is JsonArray ? SchemaArrayChildren(node["items"]) : [(node["items"], false)];
    }

    static bool IsDefinitionKeyword(string keyword) => keyword == "$defs" || keyword == "definitions";
}

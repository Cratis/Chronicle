// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    static bool IsNonNullMetadataComposition(JsonSchema candidate)
    {
        if (candidate.Type == JsonObjectType.Null) return false;
        var target = StringValue(candidate.Node, "$ref") is { } reference ? candidate.MetadataTarget(reference, scoped: false) : null;
        return target is null || candidate.MetadataView(target).Type != JsonObjectType.Null;
    }

    static IEnumerable<(JsonObject Node, JsonSchema Schema)> ChargedCachedChildren(IEnumerable<(JsonObject Node, JsonSchema Schema)> children, Func<bool> charge)
    {
        foreach (var child in children)
        {
            if (!charge()) yield break;
            yield return child;
        }
    }

    Dictionary<JsonObject, JsonSchema> CachedMetadataChildren(bool includeChildren, bool contextual, Func<bool> charge)
    {
        // Facades can be annotated while building a schema without updating their parent's raw JSON.
        // Read those existing facades, but key visitation by their original nodes, not their clones.
        var result = new Dictionary<JsonObject, JsonSchema>(ReferenceEqualityComparer.Instance);
        foreach (var (node, child) in ExistingMetadataChildren(includeChildren, contextual, charge))
        {
            result.TryAdd(MetadataChildOrigin(node), child);
        }
        return result;
    }

    IEnumerable<(JsonObject Node, JsonSchema Schema)> ExistingMetadataChildren(bool includeChildren, bool contextual, Func<bool> charge)
    {
        var compositions = ExistingMetadataCompositions(contextual, charge);
        if (!includeChildren) return contextual ? compositions : [];
        var nodes = Node["properties"] as JsonObject;
        var properties = _propertiesCache?.Select(entry => (nodes?[entry.Key] as JsonObject ?? entry.Value.Node, (JsonSchema)entry.Value)) ?? [];
        var item = _itemCache?.Value is { } cachedItem ? new[] { (Node["items"] as JsonObject ?? cachedItem.Node, cachedItem) } : [];
        return ChargedCachedChildren(properties, charge).Concat(compositions).Concat(ChargedCachedChildren(item, charge));
    }

    IEnumerable<(JsonObject Node, JsonSchema Schema)> ExistingMetadataCompositions(bool contextual, Func<bool> charge)
    {
        var allOf = ChargedCachedChildren(CachedMetadataCompositions("allOf", _allOfCache), charge);
        var anyOf = ChargedCachedChildren(CachedMetadataCompositions("anyOf", _anyOfCache), charge);
        if (contextual) return allOf.Concat(anyOf).Concat(ChargedCachedChildren(CachedMetadataCompositions("oneOf", _oneOfCache), charge));
        if ((_allOfCache?.Count ?? (Node["allOf"] as JsonArray)?.Count ?? 0) > 0) return allOf;
        return anyOf.Where(candidate => IsNonNullMetadataComposition(candidate.Schema)).Take(1);
    }

    IEnumerable<(JsonObject Node, JsonSchema Schema)> CachedMetadataCompositions(string keyword, List<JsonSchema>? cached)
    {
        if (cached is null) yield break;
        var nodes = Node[keyword] as JsonArray;
        for (var index = 0; index < cached.Count; index++)
        {
            var node = nodes?.ElementAtOrDefault(index) as JsonObject ?? cached[index].Node;
            yield return (node, cached[index]);
        }
    }
}

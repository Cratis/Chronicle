// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Compares exact key sets and canonical full document values.</summary>
internal static class RebuildBaselineComparison
{
    /// <summary>Returns the first mismatches, rejecting empty comparisons.</summary>
    /// <param name="rebuilt">The rebuilt measurement input.</param>
    /// <param name="twin">The twin measurement input.</param>
    /// <returns>The first document differences, or an empty list when both nonempty snapshots match.</returns>
    internal static IReadOnlyList<string> Compare(IReadOnlyDictionary<string, string> rebuilt, IReadOnlyDictionary<string, string> twin)
    {
        var differences = new List<string>();
        if (rebuilt.Count == 0 || twin.Count == 0) differences.Add("No documents compared.");
        foreach (var key in rebuilt.Keys.Union(twin.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (!rebuilt.TryGetValue(key, out var actual))
            {
                differences.Add($"Missing rebuilt key: {key}");
            }
            else if (!twin.TryGetValue(key, out var expected))
            {
                differences.Add($"Unexpected rebuilt key: {key}");
            }
            else
            {
                try
                {
                    if (!JsonNode.DeepEquals(Canonical(JsonNode.Parse(actual)), Canonical(JsonNode.Parse(expected))))
                    {
                        differences.Add($"Changed value at key: {key}");
                    }
                }
                catch (InvalidOperationException error)
                {
                    differences.Add($"Invalid document at key: {key}: {error.Message}");
                }
            }
            if (differences.Count >= 10) break;
        }
        return differences;
    }

    static JsonNode? Canonical(JsonNode? node, string? property = null)
    {
        if (node is JsonObject document)
        {
            var result = new JsonObject();
            foreach (var pair in document.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (pair.Key == "__lastHandledEventSequenceNumber") continue;
                result.Add(pair.Key, Canonical(pair.Value, pair.Key));
            }
            return result;
        }
        if (node is not JsonArray array) return node?.DeepClone();
        var items = array.Select(item => Canonical(item)).ToArray();

        // Only these declared child collections have no semantic ordering key.
        if (string.Equals(property, "children", StringComparison.OrdinalIgnoreCase) || string.Equals(property, "leaves", StringComparison.OrdinalIgnoreCase))
        {
            items = items.Select(item => new { Key = ChildKey(item, property), Value = item })
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => item.Value)
                .ToArray();
        }
        return new JsonArray(items);
    }

    static string ChildKey(JsonNode? item, string? collection)
    {
        // The MongoDB sink stores a child's Id as _id.
        if (item is not JsonObject child || (child["_id"] ?? child["id"] ?? child["Id"]) is not { } key)
        {
            throw new InvalidOperationException($"Unordered collection '{collection}' contains an element without an _id/id/Id key.");
        }
        return key.ToJsonString();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// Masks the boundaries already located by compliance, without interpreting schema metadata again.
/// </summary>
internal static class FingerprintContent
{
    /// <summary>
    /// Masks a detached copy and rejects ambiguous or missing paths.
    /// </summary>
    /// <param name="input">The located plaintext intent.</param>
    /// <returns>The masked copy, or null for unsupported boundaries.</returns>
    internal static JsonObject? Mask(AppendFingerprintInput input)
    {
        var locations = input.ProtectedValues;
        if (locations.Select(value => value.Path).Distinct(StringComparer.Ordinal).Count() != locations.Count ||
            locations.Any(value => FingerprintEncoding.Name(value.Kind) is null))
        {
            return null;
        }

        var boundaries = locations.ToDictionary(value => value.Path, StringComparer.Ordinal);
        var found = new HashSet<string>(StringComparer.Ordinal);
        var content = input.Content.DeepClone().AsObject();
        return Visit(content, string.Empty, boundaries, found) && found.Count == locations.Count ? content : null;
    }

    static bool Visit(JsonNode? node, string path, Dictionary<string, ProtectedValueLocation> boundaries, HashSet<string> found)
    {
        if (node is JsonObject json)
        {
            foreach (var (name, value) in json.ToArray())
            {
                var childPath = path.Length == 0 ? name : $"{path}.{name}";
                if (boundaries.TryGetValue(childPath, out var boundary))
                {
                    if (!found.Add(childPath) || CanonicalJson.Encode(value) != CanonicalJson.Encode(boundary.Plaintext))
                    {
                        return false;
                    }

                    json[name] = new JsonObject { ["$protected"] = FingerprintEncoding.Name(boundary.Kind) };
                }
                else if (!Visit(value, childPath, boundaries, found))
                {
                    return false;
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var childPath = $"{path}[{index}]";
                if (boundaries.TryGetValue(childPath, out var boundary))
                {
                    if (!found.Add(childPath) || CanonicalJson.Encode(array[index]) != CanonicalJson.Encode(boundary.Plaintext))
                    {
                        return false;
                    }

                    array[index] = new JsonObject { ["$protected"] = FingerprintEncoding.Name(boundary.Kind) };
                }
                else if (!Visit(array[index], childPath, boundaries, found))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Compares schema evolution without changing the meaning of stored event generations.
/// </summary>
public static class JsonSchemaCompatibilityExtensions
{
    const string EnumerationKey = "enum";
    const string EnumerationNamesKey = "x-enumNames";
    const string FormatKey = "format";
    const string TitleKey = "title";

    /// <summary>
    /// Determines whether a generated schema still describes the stored data.
    /// </summary>
    /// <param name="stored">The stored schema.</param>
    /// <param name="generated">The incoming schema.</param>
    /// <returns>Whether the schemas are compatible.</returns>
    /// <remarks>
    /// Titles, nullable format markers, growing or renamed enumerations and default-only property refinements
    /// are tolerated. Protection metadata may be added, but existing metadata may not be removed or changed.
    /// Other changes require a new generation. Default-only refinements work in both directions for rolling upgrades.
    /// </remarks>
    public static bool IsCompatibleWith(this JsonSchema stored, JsonSchema generated)
    {
        var storedNode = JsonNode.Parse(stored.ToJson());
        var generatedNode = JsonNode.Parse(generated.ToJson());
        StripNullableFormatMarkers(storedNode);
        StripNullableFormatMarkers(generatedNode);
        StripTitles(storedNode);
        StripTitles(generatedNode);
        return DefaultOnlyPropertyRefinement.EraseCompatibleDifferences(storedNode, generatedNode) &&
            TryEraseCompatibleEnumerations(storedNode, generatedNode) &&
            JsonNode.DeepEquals(storedNode, generatedNode);
    }

    /// <summary>
    /// Compares schemas without treating CLR titles as a stored schema change.
    /// </summary>
    /// <param name="stored">The stored schema JSON.</param>
    /// <param name="incoming">The incoming schema JSON.</param>
    /// <returns>Whether the schemas differ only by titles.</returns>
    public static bool EqualsIgnoringTitles(string stored, string incoming)
    {
        var storedNode = JsonNode.Parse(stored);
        var incomingNode = JsonNode.Parse(incoming);
        StripTitles(storedNode);
        StripTitles(incomingNode);
        return JsonNode.DeepEquals(storedNode, incomingNode);
    }

    /// <summary>
    /// Retains typed defaulted properties when a legacy client submits their default-only representation.
    /// </summary>
    /// <param name="stored">The stored schema.</param>
    /// <param name="generated">The incoming schema.</param>
    /// <returns>The incoming schema with more precise stored properties retained.</returns>
    public static JsonSchema MorePrecise(this JsonSchema stored, JsonSchema generated)
    {
        var storedNode = JsonNode.Parse(stored.ToJson());
        var generatedNode = JsonNode.Parse(generated.ToJson())!.AsObject();
        DefaultOnlyPropertyRefinement.RetainPreciseProperties(storedNode, generatedNode);
        return new JsonSchema(generatedNode);
    }

    /// <summary>
    /// Checks whether existing protection metadata is retained in an incoming schema.
    /// </summary>
    /// <param name="stored">The stored schema.</param>
    /// <param name="generated">The incoming schema.</param>
    /// <returns>Whether no existing protection declaration was removed or changed.</returns>
    public static bool HasCompatibleProtectionMetadata(this JsonSchema stored, JsonSchema generated)
    {
        var storedNode = JsonNode.Parse(stored.ToJson());
        var generatedNode = JsonNode.Parse(generated.ToJson());
        var incoming = DefaultOnlyPropertyRefinement.Pairs(storedNode, generatedNode).ToDictionary(pair => pair.Path, pair => pair.Generated);
        foreach (var (previous, _, _, path) in DefaultOnlyPropertyRefinement.Pairs(storedNode, storedNode))
        {
            // A disappeared declaration must not bypass the protection-removal check.
            var corresponding = incoming.GetValueOrDefault(path);
            if (!DefaultOnlyPropertyRefinement.MetadataOnlyAdded(previous["compliance"], corresponding?["compliance"]) ||
                !DefaultOnlyPropertyRefinement.MetadataOnlyAdded(previous["security"], corresponding?["security"]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Finds schema paths that gained compliance or security metadata.
    /// </summary>
    /// <param name="stored">The stored schema.</param>
    /// <param name="generated">The incoming schema.</param>
    /// <returns>The paths with added protection metadata.</returns>
    public static IEnumerable<string> AddedProtectionMetadataPaths(this JsonSchema stored, JsonSchema generated) =>
        DefaultOnlyPropertyRefinement.AddedMetadataPaths(JsonNode.Parse(stored.ToJson()), JsonNode.Parse(generated.ToJson()));

    /// <summary>
    /// Strips titles from schema declarations, without visiting arbitrary default payloads or property maps.
    /// </summary>
    /// <param name="node">The schema node.</param>
    internal static void StripTitles(JsonNode? node) => DefaultOnlyPropertyRefinement.Normalize(node, schema => schema.Remove(TitleKey));

    /// <summary>
    /// Strips nullable markers from format declarations.
    /// </summary>
    /// <param name="node">The schema node.</param>
    internal static void StripNullableFormatMarkers(JsonNode? node) => DefaultOnlyPropertyRefinement.Normalize(node, schema =>
    {
        if (schema[FormatKey] is JsonValue value && value.TryGetValue<string>(out var format) && format.EndsWith('?'))
        {
            schema[FormatKey] = format[..^1];
        }
    });

    static bool TryEraseCompatibleEnumerations(JsonNode? stored, JsonNode? generated)
    {
        foreach (var (previous, incoming, _, _) in DefaultOnlyPropertyRefinement.Pairs(stored, generated))
        {
            if (!previous.ContainsKey(EnumerationKey) && !incoming.ContainsKey(EnumerationKey))
            {
                continue;
            }
            if (previous[EnumerationKey] is not JsonArray storedMembers || incoming[EnumerationKey] is not JsonArray generatedMembers)
            {
                return false;
            }
            var declared = generatedMembers.Select(JsonValues.Canonical).ToHashSet(StringComparer.Ordinal);
            if (!storedMembers.Select(JsonValues.Canonical).All(declared.Contains))
            {
                return false;
            }
            previous.Remove(EnumerationKey);
            previous.Remove(EnumerationNamesKey);
            incoming.Remove(EnumerationKey);
            incoming.Remove(EnumerationNamesKey);
        }
        return true;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for deciding whether a newly generated <see cref="JsonSchema"/> still describes the data an
/// already stored one describes.
/// </summary>
public static class JsonSchemaCompatibilityExtensions
{
    const string EnumerationKey = "enum";
    const string EnumerationNamesKey = "x-enumNames";
    const string FormatKey = "format";

    /// <summary>
    /// Determines whether a newly generated schema is a compatible evolution of an already stored one.
    /// </summary>
    /// <param name="stored">The stored <see cref="JsonSchema"/>.</param>
    /// <param name="generated">The newly generated <see cref="JsonSchema"/> to check against it.</param>
    /// <returns><see langword="true"/> when the generated schema still describes the stored data; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Everything outside the two tolerances below has to match exactly - a stored event's payload is interpreted
    /// through the schema registered for its generation, so a change to the shape silently changes the meaning of
    /// history and must go through a new generation instead.
    /// <para>
    /// The first tolerance is a nullability marker (a trailing <c>?</c> on a <c>format</c> value). It only refines
    /// how an unset value materializes, and a Chronicle upgrade can introduce it on a schema that was stored before
    /// the marker existed.
    /// </para>
    /// <para>
    /// The second is an enumeration that only gained members or had members renamed. Neither changes what an already
    /// stored value means: the member a stored value denotes keeps its underlying value, and its name is frequently
    /// not the owning application's to control in the first place - an enumeration mirroring an external system
    /// grows and gets renamed on that system's schedule. Members that <em>disappear</em> or are renumbered are a
    /// different matter, because a stored value then denotes nothing or something else, so those stay a breaking
    /// change that needs a new generation and a value map to state what the old values now mean.
    /// </para>
    /// </remarks>
    public static bool IsCompatibleWith(this JsonSchema stored, JsonSchema generated)
    {
        var storedNode = JsonNode.Parse(stored.ToJson());
        var generatedNode = JsonNode.Parse(generated.ToJson());

        StripNullableFormatMarkers(storedNode);
        StripNullableFormatMarkers(generatedNode);

        return TryEraseCompatibleEnumerations(storedNode, generatedNode) &&
            storedNode?.ToJsonString() == generatedNode?.ToJsonString();
    }

    /// <summary>
    /// Strips every nullability marker - a trailing <c>?</c> appended to a <c>format</c> value - from a schema node.
    /// </summary>
    /// <param name="node">The <see cref="JsonNode"/> to strip, which may be <see langword="null"/>.</param>
    internal static void StripNullableFormatMarkers(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (jsonObject[FormatKey] is JsonValue formatValue &&
                    formatValue.TryGetValue<string>(out var format) &&
                    format.EndsWith('?'))
                {
                    jsonObject[FormatKey] = format[..^1];
                }

                foreach (var property in jsonObject.ToArray().Where(_ => _.Key != FormatKey))
                {
                    StripNullableFormatMarkers(property.Value);
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray.ToArray())
                {
                    StripNullableFormatMarkers(item);
                }

                break;
        }
    }

    /// <summary>
    /// Walks two schema nodes in lockstep and removes every enumeration declaration the two agree on, so that what
    /// is left can be compared verbatim.
    /// </summary>
    /// <param name="stored">The node from the stored schema.</param>
    /// <param name="generated">The node from the generated schema.</param>
    /// <returns><see langword="false"/> as soon as an enumeration is found that lost or renumbered a member; otherwise <see langword="true"/>.</returns>
    /// <remarks>
    /// Only nodes present on both sides are visited. A node present on one side alone is a difference the caller's
    /// verbatim comparison catches on its own, and erasing an enumeration on one side without the other would hide it.
    /// </remarks>
    static bool TryEraseCompatibleEnumerations(JsonNode? stored, JsonNode? generated)
    {
        switch (stored)
        {
            case JsonObject storedObject when generated is JsonObject generatedObject:
                if (storedObject.ContainsKey(EnumerationKey) || generatedObject.ContainsKey(EnumerationKey))
                {
                    if (!EnumerationOnlyGrewOrWasRenamed(storedObject, generatedObject))
                    {
                        return false;
                    }

                    EraseEnumeration(storedObject);
                    EraseEnumeration(generatedObject);
                }

                return storedObject
                    .ToArray()
                    .Where(entry => generatedObject.ContainsKey(entry.Key))
                    .All(entry => TryEraseCompatibleEnumerations(entry.Value, generatedObject[entry.Key]));

            case JsonArray storedArray when generated is JsonArray generatedArray:
                return Enumerable
                    .Range(0, Math.Min(storedArray.Count, generatedArray.Count))
                    .All(index => TryEraseCompatibleEnumerations(storedArray[index], generatedArray[index]));

            default:
                return true;
        }
    }

    /// <summary>
    /// Gets whether two enumeration declarations differ only by members the generated one added, or by member names.
    /// </summary>
    /// <param name="stored">The node from the stored schema.</param>
    /// <param name="generated">The node from the generated schema.</param>
    /// <returns><see langword="true"/> when every stored member is still declared; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Member names are deliberately not compared at all - the underlying value is what a stored payload carries, so
    /// a rename leaves every stored value denoting exactly what it denoted before.
    /// </remarks>
    static bool EnumerationOnlyGrewOrWasRenamed(JsonObject stored, JsonObject generated)
    {
        if (stored[EnumerationKey] is not JsonArray storedMembers ||
            generated[EnumerationKey] is not JsonArray generatedMembers)
        {
            return false;
        }

        var declared = generatedMembers.Select(JsonValues.Canonical).ToHashSet(StringComparer.Ordinal);
        return storedMembers.Select(JsonValues.Canonical).All(declared.Contains);
    }

    static void EraseEnumeration(JsonObject node)
    {
        node.Remove(EnumerationKey);
        node.Remove(EnumerationNamesKey);
    }
}

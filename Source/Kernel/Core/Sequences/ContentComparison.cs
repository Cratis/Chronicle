// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Compares content in the schema-guided JSON representation used by event storage.
/// </summary>
internal static class ContentComparison
{
    /// <summary>
    /// Compares strictly released stored content with the attempted append's storage representation.
    /// </summary>
    /// <param name="stored">The complete released document.</param>
    /// <param name="attempted">The prepared append.</param>
    /// <param name="schema">The requested generation's schema.</param>
    /// <param name="converter">The converter used by append and storage.</param>
    /// <returns>Whether the canonical documents match.</returns>
    internal static bool Equals(JsonObject stored, JsonObject attempted, JsonSchema schema, IExpandoObjectConverter converter)
    {
        // Append first converts JSON to an expando using the generation schema. MongoDB then
        // converts that expando back to JSON before BSON storage. Use that representation on
        // every backend: enum names, formatted dates and floating-point storage precision.
        var expected = converter.ToJsonObject(converter.ToExpandoObject(attempted, schema), schema);
        var actual = converter.ToJsonObject(converter.ToExpandoObject(stored, schema), schema);

        // Only the attempted append may discard undeclared provider properties. A stored member
        // must not disappear merely because today's schema cannot describe it.
        PreserveStoredMembers(stored, actual);
        NormalizeStoragePrecision(actual);
        NormalizeStoragePrecision(expected);
        return JsonNode.DeepEquals(actual, expected);
    }

    static void NormalizeStoragePrecision(JsonNode node)
    {
        // BSON stores JSON decimal fractions as doubles. Applying the same conversion to both
        // documents makes equality backend-independent, but cannot detect sub-double differences.
        // Parse the JSON text like the BSON reader: a decimal-to-double cast can round differently.
        if (node is JsonObject document)
        {
            foreach (var (name, value) in document.ToArray())
            {
                if (value is JsonValue scalar && scalar.TryGetValue<decimal>(out _))
                {
                    document[name] = double.Parse(scalar.ToJsonString(), CultureInfo.InvariantCulture);
                }
                else if (value is not null)
                {
                    NormalizeStoragePrecision(value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue scalar && scalar.TryGetValue<decimal>(out _))
                {
                    array[index] = double.Parse(scalar.ToJsonString(), CultureInfo.InvariantCulture);
                }
                else if (array[index] is { } value)
                {
                    NormalizeStoragePrecision(value);
                }
            }
        }
    }

    static void PreserveStoredMembers(JsonNode stored, JsonNode canonical)
    {
        if (stored is JsonObject document && canonical is JsonObject result)
        {
            foreach (var (name, value) in document)
            {
                if (!result.TryGetPropertyValue(name, out var normalized))
                {
                    result[name] = value?.DeepClone();
                }
                else if (value is not null && normalized is not null)
                {
                    PreserveStoredMembers(value, normalized);
                }
            }
        }
        else if (stored is JsonArray array && canonical is JsonArray normalizedArray)
        {
            for (var index = 0; index < Math.Min(array.Count, normalizedArray.Count); index++)
            {
                if (array[index] is { } value && normalizedArray[index] is { } normalized)
                {
                    PreserveStoredMembers(value, normalized);
                }
            }
        }
    }
}

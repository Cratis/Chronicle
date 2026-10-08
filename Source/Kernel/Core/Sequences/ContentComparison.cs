// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Json;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Compares stored JSON without passing historical content through a lossy schema conversion.
/// </summary>
internal static class ContentComparison
{
    /// <summary>
    /// Produces the backend's actual append representation only when conversion preserves its values.
    /// </summary>
    /// <param name="attempted">The attempted document, with protected values masked.</param>
    /// <param name="schema">The generation schema.</param>
    /// <param name="converter">The append converter.</param>
    /// <param name="storage">The selected backend.</param>
    /// <returns>The stored representation, or null when conversion loses information.</returns>
    internal static JsonObject? Prepare(JsonObject attempted, JsonSchema schema, IExpandoObjectConverter converter, IEventSequenceStorage storage)
    {
        var content = converter.ToExpandoObject(attempted, schema);
        if (!PreservesConversion(attempted, content, schema))
        {
            return null;
        }

        return Prepare(content, schema, converter, storage);
    }

    /// <summary>
    /// Checks the raw migration output before schema conversion can discard precision.
    /// </summary>
    /// <param name="source">The raw JSON.</param>
    /// <param name="converted">The converted content.</param>
    /// <param name="schema">The conversion schema.</param>
    /// <returns>Whether every declared non-null value survives conversion.</returns>
    internal static bool PreservesConversion(JsonObject source, ExpandoObject converted, JsonSchema schema) =>
        PreservesValues(source, JsonSerializer.SerializeToNode(converted, Globals.JsonSerializerOptions), schema);

    /// <summary>
    /// Checks the backend representation of already loss-checked migration content.
    /// </summary>
    /// <param name="content">The schema-converted content.</param>
    /// <param name="schema">The generation schema.</param>
    /// <param name="converter">The append converter.</param>
    /// <param name="storage">The selected backend.</param>
    /// <returns>The stored representation, or null when serialization loses information.</returns>
    internal static JsonObject? Prepare(ExpandoObject content, JsonSchema schema, IExpandoObjectConverter converter, IEventSequenceStorage storage)
    {
        var before = JsonSerializer.SerializeToNode(content, Globals.JsonSerializerOptions);
        var serialized = storage.SerializeContentForVerification(content, schema);
        if (serialized is null || JsonNode.Parse(serialized) is not JsonObject expected)
        {
            return null;
        }

        // A backend may encode enums as names, but it must round-trip the same value. This also
        // detects MongoDB decimal -> double rounding and unknown enum -> default substitution.
        var roundTripped = converter.ToExpandoObject(expected, schema);
        var after = JsonSerializer.SerializeToNode(roundTripped, Globals.JsonSerializerOptions);
        return Equals(before, after) ? expected : null;
    }

    /// <summary>
    /// Compares JSON values exactly, including offsets in strings and every digit in numbers.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Whether the JSON values are equal.</returns>
    internal static bool Equals(JsonNode? left, JsonNode? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left.GetValueKind() != right.GetValueKind())
        {
            return false;
        }

        return (left, right) switch
        {
            (JsonObject a, JsonObject b) => a.Count == b.Count && a.All(property => b.TryGetPropertyValue(property.Key, out var value) && Equals(property.Value, value)),
            (JsonArray a, JsonArray b) => a.Count == b.Count && a.Zip(b).All(pair => Equals(pair.First, pair.Second)),
            _ when left.GetValueKind() == JsonValueKind.Number => Number(left.ToJsonString()) == Number(right.ToJsonString()),

            // Serialize first: JsonValue<DateTimeOffset>.Equals compares instants, losing offsets.
            _ => JsonElement.DeepEquals(JsonSerializer.SerializeToElement(left), JsonSerializer.SerializeToElement(right))
        };
    }

    static bool PreservesValues(JsonNode? source, JsonNode? converted, JsonSchema schema)
    {
        if (source is JsonObject document && converted is JsonObject result)
        {
            // Append intentionally ignores undeclared properties and substitutes schema defaults
            // for nulls. A NON-null declared value disappearing is loss, not a default to compare.
            var properties = schema.GetFlattenedProperties().ToArray();
            return document.All(property =>
            {
                var definition = properties.FirstOrDefault(_ => _.Name == property.Key) ??
                    properties.FirstOrDefault(_ => _.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase));
                if (property.Value is null || (properties.Length > 0 && definition is null))
                {
                    return true;
                }

                return result.TryGetPropertyValue(definition?.Name ?? property.Key, out var value) &&
                    PreservesValues(property.Value, value, definition ?? new JsonSchema());
            });
        }

        if (source is JsonArray array && converted is JsonArray convertedArray)
        {
            var item = schema.Item ?? new JsonSchema();
            return array.Count == convertedArray.Count && array.Zip(convertedArray).All(pair => PreservesValues(pair.First, pair.Second, item));
        }

        return Equals(source, converted);
    }

    static (BigInteger Significand, BigInteger Exponent) Number(string token)
    {
        var exponentIndex = token.IndexOfAny(['e', 'E']);
        var mantissa = exponentIndex < 0 ? token : token[..exponentIndex];
        var exponent = exponentIndex < 0 ? BigInteger.Zero : BigInteger.Parse(token[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
        var point = mantissa.IndexOf('.');
        if (point >= 0)
        {
            exponent -= mantissa.Length - point - 1;
            mantissa = mantissa.Remove(point, 1);
        }

        var trimmed = mantissa.TrimEnd('0');
        exponent += mantissa.Length - trimmed.Length;
        var significand = (trimmed.Length == 0 || trimmed == "-") ? BigInteger.Zero : BigInteger.Parse(trimmed, CultureInfo.InvariantCulture);
        return (significand, significand.IsZero ? BigInteger.Zero : exponent);
    }
}

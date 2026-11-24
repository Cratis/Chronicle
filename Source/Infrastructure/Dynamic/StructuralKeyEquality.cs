// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Concepts;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Compares complete materialized identities without trusting object equality or lossy name normalization.
/// </summary>
internal static class StructuralKeyEquality
{
    const int MaximumDepth = 64;

    /// <summary>
    /// Checks whether an identity requires structural comparison rather than scalar conversion.
    /// </summary>
    /// <param name="value">The identity, possibly wrapped in a concept.</param>
    /// <returns>Whether the identity has a structured materialization.</returns>
    internal static bool IsStructured(object? value)
    {
        if (value?.IsConcept() == true) return IsStructured(value.GetConceptValue());
        return value switch
        {
            null => false,
            string text => LooksLikeJson(text),
            JsonElement element => element.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
            JsonValue => false,
            _ => !IsScalar(value)
        };
    }

    /// <summary>
    /// Compares validated identity shapes and their normalized leaves symmetrically.
    /// </summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <returns>Whether both identities are unambiguous and have the same complete value.</returns>
    internal static bool AreEqual(object? left, object? right)
    {
        try
        {
            return TryNormalizeRoot(left, out var normalizedLeft) &&
                TryNormalizeRoot(right, out var normalizedRight) &&
                HaveSameValue(normalizedLeft, normalizedRight);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or TargetInvocationException)
        {
            // Malformed JSON, cycles and unsupported materializations cannot identify a child safely.
            return false;
        }
    }

    static bool LooksLikeJson(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        return !trimmed.IsEmpty && trimmed[0] is '{' or '[';
    }

    static bool IsScalar(object value) => value is string or Guid or decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Enum || value.GetType().IsPrimitive;

    static bool TryNormalizeRoot(object? value, out object? normalized)
    {
        if (value?.IsConcept() == true) return TryNormalizeRoot(value.GetConceptValue(), out normalized);
        if (value is string text && LooksLikeJson(text))
        {
            using var document = JsonDocument.Parse(text);
            return TryNormalize(document.RootElement, 0, out normalized);
        }
        return TryNormalize(value, 0, out normalized);
    }

    static bool TryNormalize(object? value, int depth, out object? normalized)
    {
        normalized = null;
        if (depth > MaximumDepth) return false;
        if (value is null) return true;
        if (value.IsConcept()) return TryNormalize(value.GetConceptValue(), depth + 1, out normalized);
        return value switch
        {
            JsonElement element => TryNormalizeElement(element, depth, out normalized),
            JsonNode node => TryNormalizeElement(JsonSerializer.SerializeToElement(node), depth, out normalized),
            _ => TryNormalizeMaterialization(value, depth, out normalized)
        };
    }

    static bool TryNormalizeMaterialization(object value, int depth, out object? normalized) =>
        value switch
        {
            IDictionary<string, object?> properties => TryNormalizeProperties(properties, depth, out normalized),
            IDictionary dictionary => TryNormalizeDictionary(dictionary, depth, out normalized),
            string text => NormalizeString(text, out normalized),
            IEnumerable values => TryNormalizeArray(values.Cast<object?>(), depth, out normalized),
            _ when IsScalar(value) => TryNormalizeElement(JsonSerializer.SerializeToElement(value), depth, out normalized),
            _ => TryNormalizeProperties(ReadProperties(value), depth, out normalized)
        };

    static IEnumerable<KeyValuePair<string, object?>> ReadProperties(object value) =>
        value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .Select(property => KeyValuePair.Create(property.Name, property.GetValue(value)));

    static bool TryNormalizeDictionary(IDictionary dictionary, int depth, out object? normalized)
    {
        normalized = null;
        var keys = dictionary.Keys.Cast<object>().ToArray();
        if (keys.Any(key => key is not string)) return false;
        return TryNormalizeProperties(keys.Select(key => KeyValuePair.Create((string)key, dictionary[key])), depth, out normalized);
    }

    static bool TryNormalizeProperties(IEnumerable<KeyValuePair<string, object?>> properties, int depth, out object? normalized)
    {
        normalized = null;
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in properties)
        {
            if (result.ContainsKey(name) || !TryNormalize(value, depth + 1, out var normalizedValue)) return false;
            result.Add(name, normalizedValue);
        }
        normalized = result;
        return true;
    }

    static bool TryNormalizeArray(IEnumerable<object?> values, int depth, out object? normalized)
    {
        normalized = null;
        var result = new List<object?>();
        foreach (var value in values)
        {
            if (!TryNormalize(value, depth + 1, out var normalizedValue)) return false;
            result.Add(normalizedValue);
        }
        normalized = result;
        return true;
    }

    static bool TryNormalizeElement(JsonElement element, int depth, out object? normalized)
    {
        normalized = null;
        return element.ValueKind switch
        {
            JsonValueKind.Object => TryNormalizeProperties(element.EnumerateObject().Select(property => KeyValuePair.Create<string, object?>(property.Name, property.Value)), depth, out normalized),
            JsonValueKind.Array => TryNormalizeArray(element.EnumerateArray().Cast<object?>(), depth, out normalized),
            _ => TryNormalizeJsonScalar(element, out normalized)
        };
    }

    static bool TryNormalizeJsonScalar(JsonElement element, out object? normalized)
    {
        normalized = null;
        return element.ValueKind switch
        {
            JsonValueKind.String => NormalizeString(element.GetString()!, out normalized),
            JsonValueKind.Number => PreserveNumber(element, out normalized),
            JsonValueKind.True or JsonValueKind.False => NormalizeBoolean(element, out normalized),
            JsonValueKind.Null => true,
            _ => false
        };
    }

    static bool NormalizeString(string value, out object? normalized)
    {
        normalized = value.Length == 36 && Guid.TryParseExact(value, "D", out var guid) ? guid : value;
        return true;
    }

    static bool PreserveNumber(JsonElement element, out object? normalized)
    {
        normalized = element.Clone();
        return true;
    }

    static bool NormalizeBoolean(JsonElement element, out object? normalized)
    {
        normalized = element.GetBoolean();
        return true;
    }

    static bool HaveSameValue(object? left, object? right) => left switch
    {
        JsonElement number => right is JsonElement other && JsonElement.DeepEquals(number, other),
        Dictionary<string, object?> properties => HaveSameProperties(properties, right),
        List<object?> values => HaveSameItems(values, right),
        _ => Equals(left, right)
    };

    static bool HaveSameProperties(Dictionary<string, object?> properties, object? right) =>
        right is Dictionary<string, object?> other && properties.Count == other.Count &&
            properties.All(property => other.TryGetValue(property.Key, out var value) && HaveSameValue(property.Value, value));

    static bool HaveSameItems(List<object?> values, object? right) =>
        right is List<object?> other && values.Count == other.Count &&
            values.Zip(other).All(pair => HaveSameValue(pair.First, pair.Second));
}

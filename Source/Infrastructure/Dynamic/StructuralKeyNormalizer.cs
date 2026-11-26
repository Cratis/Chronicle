// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Concepts;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Validates identity materializations and produces complete, comparable value trees.
/// </summary>
internal static class StructuralKeyNormalizer
{
    /// <summary>
    /// Gets the sentinel for an ambiguous or unsupported identity.
    /// </summary>
    internal static object Unsupported { get; } = new();

    /// <summary>
    /// Normalizes an identity, interpreting valid object or array JSON text only at the root.
    /// </summary>
    /// <param name="value">The identity to normalize.</param>
    /// <returns>The normalized identity, or <see cref="Unsupported"/>.</returns>
    internal static object? NormalizeRoot(object? value)
    {
        if (value?.IsConcept() == true) return NormalizeRoot(value.GetConceptValue());
        return NormalizeRootValue(value);
    }

    /// <summary>
    /// Reads JSON structure text while leaving non-JSON scalar strings unchanged.
    /// </summary>
    /// <param name="text">The possible object or array JSON text.</param>
    /// <returns>A document owned by the caller, or null when the text is not JSON structure.</returns>
    internal static JsonDocument? ReadStructure(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        if (trimmed.IsEmpty || trimmed[0] is not ('{' or '[')) return null;
        return ParseStructure(text);
    }

    /// <summary>
    /// Checks whether a CLR value is a scalar leaf rather than a property-based identity.
    /// </summary>
    /// <param name="value">The value to classify.</param>
    /// <returns>Whether the value is a scalar.</returns>
    internal static bool IsScalar(object value) => value is string or Guid or decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Enum || value.GetType().IsPrimitive;

    /// <summary>
    /// Normalizes a value within a validated traversal scope.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="scope">The current traversal scope.</param>
    /// <returns>The normalized value, or <see cref="Unsupported"/>.</returns>
    internal static object? Normalize(object? value, NormalizationScope scope)
    {
        if (value is null) return null;
        if (value.IsConcept()) return scope.NormalizeChild(value.GetConceptValue());
        return value switch
        {
            JsonElement element => NormalizeElement(element, scope),
            JsonNode node => NormalizeElement(JsonSerializer.SerializeToElement(node), scope),
            _ => NormalizeMaterialization(value, scope)
        };
    }

    static object? NormalizeRootValue(object? value)
    {
        if (value is string text)
        {
            using var document = ReadStructure(text);
            if (document is not null) return NormalizationScope.Root.Normalize(document.RootElement);
        }
        return NormalizationScope.Root.Normalize(value);
    }

    static JsonDocument? ParseStructure(string text)
    {
        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static object? NormalizeMaterialization(object value, NormalizationScope scope) => value switch
    {
        IDictionary<string, object?> properties => NormalizeProperties(properties, scope),
        IDictionary dictionary => NormalizeDictionary(dictionary, scope),
        string text => NormalizeString(text),
        IEnumerable values => NormalizeArray(values.Cast<object?>(), scope),
        _ when IsScalar(value) => NormalizeElement(JsonSerializer.SerializeToElement(value), scope),
        _ => NormalizeProperties(ReadProperties(value), scope)
    };

    static IEnumerable<KeyValuePair<string, object?>> ReadProperties(object value) =>
        value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .Select(property => KeyValuePair.Create(property.Name, property.GetValue(value)));

    static object NormalizeDictionary(IDictionary dictionary, NormalizationScope scope)
    {
        var keys = dictionary.Keys.Cast<object>().ToArray();
        if (keys.Any(key => key is not string)) return Unsupported;
        return NormalizeProperties(keys.Select(key => KeyValuePair.Create((string)key, dictionary[key])), scope);
    }

    static object NormalizeProperties(IEnumerable<KeyValuePair<string, object?>> properties, NormalizationScope scope)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in properties)
        {
            if (result.ContainsKey(name)) return Unsupported;
            var normalized = scope.NormalizeChild(value);
            if (ReferenceEquals(normalized, Unsupported)) return Unsupported;
            result.Add(name, normalized);
        }
        return result;
    }

    static object NormalizeArray(IEnumerable<object?> values, NormalizationScope scope)
    {
        var result = new List<object?>();
        foreach (var value in values)
        {
            var normalized = scope.NormalizeChild(value);
            if (ReferenceEquals(normalized, Unsupported)) return Unsupported;
            result.Add(normalized);
        }
        return result;
    }

    static object? NormalizeElement(JsonElement element, NormalizationScope scope) => element.ValueKind switch
    {
        JsonValueKind.Object => NormalizeProperties(element.EnumerateObject().Select(property => KeyValuePair.Create<string, object?>(property.Name, property.Value)), scope),
        JsonValueKind.Array => NormalizeArray(element.EnumerateArray().Cast<object?>(), scope),
        _ => NormalizeJsonScalar(element)
    };

    static object? NormalizeJsonScalar(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => NormalizeString(element.GetString()!),
        JsonValueKind.Number => PreserveNumber(element),
        JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
        JsonValueKind.Null => null,
        _ => Unsupported
    };

    /// <summary>
    /// Owns the exact JSON number after its source document is disposed, without decimal or double conversion.
    /// </summary>
    /// <param name="element">The original JSON number.</param>
    /// <returns>An owned copy retaining the number's exact representation.</returns>
    static JsonElement PreserveNumber(JsonElement element) => element.Clone();

    static object NormalizeString(string value) => value.Length == 36 && Guid.TryParseExact(value, "D", out var guid) ? guid : value;
}

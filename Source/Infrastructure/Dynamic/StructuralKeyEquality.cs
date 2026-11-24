// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Concepts;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Compares complete materialized identities without trusting object equality or lossy name normalization.
/// </summary>
/// <remarks>
/// Non-scalar CLR types are compared by their public readable instance properties; custom Equals implementations are ignored.
/// </remarks>
internal static class StructuralKeyEquality
{
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
            string text => IsJsonStructure(text),
            JsonElement element => element.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
            JsonValue => false,
            _ => !StructuralKeyNormalizer.IsScalar(value)
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
            var normalizedLeft = StructuralKeyNormalizer.NormalizeRoot(left);
            if (ReferenceEquals(normalizedLeft, StructuralKeyNormalizer.Unsupported)) return false;
            var normalizedRight = StructuralKeyNormalizer.NormalizeRoot(right);
            return !ReferenceEquals(normalizedRight, StructuralKeyNormalizer.Unsupported) && HaveSameValue(normalizedLeft, normalizedRight);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or TargetInvocationException)
        {
            // Malformed JSON, cycles and unsupported materializations cannot identify a child safely.
            return false;
        }
    }

    static bool IsJsonStructure(string text)
    {
        using var document = StructuralKeyNormalizer.ReadStructure(text);
        return document is not null;
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

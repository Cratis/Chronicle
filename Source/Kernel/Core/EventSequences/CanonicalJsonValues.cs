// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Writes known CLR values without serializing and parsing an intermediate document.
/// Unknown types, converters, invalid UTF-16 and deep graphs retain the serialize/parse path.
/// </summary>
static class CanonicalJsonValues
{
    /// <summary>
    /// Determines whether the complete graph can bypass JSON normalization safely.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="depth">The containing JSON depth.</param>
    /// <returns>Whether direct writing preserves the persisted representation.</returns>
    internal static bool CanWrite(object? value, int depth = 0) => value switch
    {
        ExpandoObject properties => CanWriteProperties(properties, depth),
        Dictionary<string, object?> properties when value.GetType() == typeof(Dictionary<string, object?>) => CanWriteProperties(properties, depth),
        object?[] items when IsKnownArray(value.GetType()) => CanWriteItems(items, depth),
        List<object?> items when value.GetType() == typeof(List<object?>) => CanWriteItems(items, depth),
        _ => CanWriteScalar(value)
    };

    /// <summary>
    /// Writes a graph already accepted by <see cref="CanWrite"/>.
    /// </summary>
    /// <param name="writer">The destination.</param>
    /// <param name="value">The accepted value.</param>
    /// <param name="options">The canonical serializer options.</param>
    internal static void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case IDictionary<string, object?> properties:
                WriteProperties(writer, properties, options);
                break;
            case IEnumerable<object?> items:
                writer.WriteStartArray();
                foreach (var item in items) Write(writer, item, options);
                writer.WriteEndArray();
                break;
            default:
                // Keep the serializer's exact numeric and temporal formatting, including decimal scale
                // and negative zero. These known scalars need no JSON normalization or property sorting.
                JsonSerializer.Serialize(writer, value, options);
                break;
        }
    }

    static bool IsKnownArray(Type type) => type == typeof(object[]) || type == typeof(string[]) || type == typeof(ExpandoObject[]);

    static bool CanWriteScalar(object? value) => value switch
    {
        null => true,
        string text => IsWellFormed(text),
        char character => !char.IsSurrogate(character),
        double number => double.IsFinite(number),
        float number => float.IsFinite(number),
        Half number => Half.IsFinite(number),
        bool or byte or sbyte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or decimal or
            DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid or byte[] => true,
        _ => false
    };

    static bool CanWriteProperties(IDictionary<string, object?> properties, int depth)
    {
        // Leave room for the serializer's scalar write at the deepest level.
        if (depth >= 63) return false;
        foreach (var property in properties)
        {
            if (!IsWellFormed(property.Key) || !CanWrite(property.Value, depth + 1)) return false;
        }
        return true;
    }

    static bool CanWriteItems(IEnumerable<object?> items, int depth)
    {
        if (depth >= 63) return false;
        foreach (var item in items)
        {
            if (!CanWrite(item, depth + 1)) return false;
        }
        return true;
    }

    static void WriteProperties(Utf8JsonWriter writer, IDictionary<string, object?> values, JsonSerializerOptions options)
    {
        // A custom dictionary comparer can admit multiple string instances with identical text.
        // Original position preserves the serialize/parse path's stable duplicate-name ordering.
        var properties = new (KeyValuePair<string, object?> Property, int Position)[values.Count];
        var index = 0;
        foreach (var property in values)
        {
            properties[index] = (property, index);
            index++;
        }
        Array.Sort(properties, CompareProperties);
        writer.WriteStartObject();
        foreach (var (property, _) in properties)
        {
            writer.WritePropertyName(property.Key);
            Write(writer, property.Value, options);
        }
        writer.WriteEndObject();
    }

    static int CompareProperties((KeyValuePair<string, object?> Property, int Position) left, (KeyValuePair<string, object?> Property, int Position) right)
    {
        var byName = StringComparer.Ordinal.Compare(left.Property.Key, right.Property.Key);
        return byName != 0 ? byName : left.Position.CompareTo(right.Position);
    }

    static bool IsWellFormed(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsSurrogate(value[index])) continue;
            if (!char.IsHighSurrogate(value[index])) return false;
            if (++index == value.Length) return false;
            if (!char.IsLowSurrogate(value[index])) return false;
        }
        return true;
    }
}

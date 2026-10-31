// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The shared ordinal-sorted JSON encoding used by persisted event hashes and append fingerprints.
/// </summary>
internal static class CanonicalJson
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static readonly JsonWriterOptions _jsonWriterOptions = new()
    {
        Indented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Encodes a value using the persisted canonical representation.
    /// </summary>
    /// <param name="content">The value.</param>
    /// <returns>The canonical text.</returns>
    internal static string Encode(object? content)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Write(content, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes canonical JSON without altering number or array representations.
    /// </summary>
    /// <param name="content">The value.</param>
    /// <param name="buffer">The destination.</param>
    internal static void Write(object? content, IBufferWriter<byte> buffer)
    {
        var utf8 = JsonSerializer.SerializeToUtf8Bytes(content, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(utf8);
        using var writer = new Utf8JsonWriter(buffer, _jsonWriterOptions);
        WriteSorted(document.RootElement, writer);
        writer.Flush();
    }

    /// <summary>
    /// Orders the properties of an object by name, ordinally, keeping the original order of equal names.
    /// </summary>
    /// <param name="element">The object.</param>
    /// <returns>The properties in canonical order.</returns>
    /// <remarks>
    /// The same order a stable ordinal sort gives. Hashing runs for every appended event, so this avoids the
    /// allocations of a LINQ ordering, and the sort altogether when the properties already are in order.
    /// </remarks>
    static (string Name, JsonElement Value)[] SortedProperties(JsonElement element)
    {
        var properties = new (string Name, JsonElement Value)[element.GetPropertyCount()];
        var count = 0;
        var sorted = true;
        foreach (var property in element.EnumerateObject())
        {
            var name = property.Name;
            if (count > 0 && string.CompareOrdinal(properties[count - 1].Name, name) > 0) sorted = false;
            properties[count++] = (name, property.Value);
        }

        if (!sorted)
        {
            var positions = new int[count];
            for (var index = 0; index < count; index++) positions[index] = index;
            var unsorted = properties;
            Array.Sort(positions, (left, right) =>
            {
                var byName = string.CompareOrdinal(unsorted[left].Name, unsorted[right].Name);
                return byName != 0 ? byName : left.CompareTo(right);
            });
            properties = [.. positions.Select(position => unsorted[position])];
        }

        return properties;
    }

    static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in SortedProperties(element))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSorted(property.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSorted(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}

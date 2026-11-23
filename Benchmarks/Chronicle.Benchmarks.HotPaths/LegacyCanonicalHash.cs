// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Benchmark-only copy of the pre-Stage-A event hash algorithm. Keep this unchanged as the comparison baseline.
/// </summary>
static class LegacyCanonicalHash
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Calculates the persisted hash with the unmodified legacy algorithm.
    /// </summary>
    /// <param name="eventTypeId">The event type.</param>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="content">The content generation.</param>
    /// <returns>The legacy hash.</returns>
    internal static EventHash Calculate(EventTypeId eventTypeId, EventSourceId eventSourceId, ExpandoObject content)
    {
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write(Encoding.UTF8.GetBytes($"{eventTypeId.Value}|{eventSourceId.Value}|"));
        Write(content, buffer);
        var hashBytes = SHA256.HashData(buffer.WrittenSpan);
        return Convert.ToBase64String(hashBytes);
    }

    static void Write(object? content, IBufferWriter<byte> buffer)
    {
        var utf8 = JsonSerializer.SerializeToUtf8Bytes(content, _serializerOptions);
        using var document = JsonDocument.Parse(utf8);
        using var writer = new Utf8JsonWriter(buffer, _writerOptions);
        WriteSorted(document.RootElement, writer);
        writer.Flush();
    }

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

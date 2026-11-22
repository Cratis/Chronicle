// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Benchmark-only copy of the Stage-A canonical writer and hashing pipeline from 2251cf8f6c.
/// Uses the unchanged pooled byte buffer, but always serializes and parses before sorting.
/// </summary>
static class StageACanonicalHash
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
    /// Calculates the persisted hash with the unchanged Stage-A pipeline.
    /// </summary>
    /// <param name="eventTypeId">The event type.</param>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="content">The content.</param>
    /// <returns>The hash.</returns>
    public static EventHash Calculate(EventTypeId eventTypeId, EventSourceId eventSourceId, ExpandoObject content)
    {
        using var buffer = new CanonicalJsonBuffer();
        WritePrefixPart(eventTypeId.Value, buffer);
        WritePrefixPart(eventSourceId.Value, buffer);
        Write(content, buffer);
        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(buffer.WrittenSpan, hashBytes);
        return Convert.ToBase64String(hashBytes);
    }

    static void WritePrefixPart(string? value, CanonicalJsonBuffer buffer)
    {
        var destination = buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(value?.Length ?? 0) + 1);
        var written = Encoding.UTF8.GetBytes(value.AsSpan(), destination);
        destination[written] = (byte)'|';
        buffer.Advance(written + 1);
    }

    static void Write(object? content, IBufferWriter<byte> buffer)
    {
        var utf8 = JsonSerializer.SerializeToUtf8Bytes(content, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(utf8);
        using var writer = new Utf8JsonWriter(buffer, _jsonWriterOptions);
        WriteSorted(document.RootElement, writer);
        writer.Flush();
    }

    static void WriteObject(JsonElement element, Utf8JsonWriter writer)
    {
        var nameCapacity = 0;
        foreach (var property in element.EnumerateObject()) nameCapacity += JsonMarshal.GetRawUtf8PropertyName(property).Length;
        var names = ArrayPool<char>.Shared.Rent(nameCapacity);
        try
        {
            foreach (var property in SortedProperties(element, names))
            {
                writer.WritePropertyName(names.AsSpan(property.Start, property.Length));
                WriteSorted(property.Value, writer);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(names, clearArray: true);
        }
    }

    static (int Start, int Length, int Position, JsonElement Value)[] SortedProperties(JsonElement element, char[] names)
    {
        var properties = new (int Start, int Length, int Position, JsonElement Value)[element.GetPropertyCount()];
        var count = 0;
        var offset = 0;
        var sorted = true;
        foreach (var property in element.EnumerateObject())
        {
            var length = CopyName(property, names.AsSpan(offset));
            if (count > 0 && names.AsSpan(properties[count - 1].Start, properties[count - 1].Length).SequenceCompareTo(names.AsSpan(offset, length)) > 0) sorted = false;
            properties[count] = (offset, length, count, property.Value);
            count++;
            offset += length;
        }

        if (!sorted) SortProperties(properties, names);
        return properties;
    }

    static void SortProperties((int Start, int Length, int Position, JsonElement Value)[] properties, char[] names) =>
        Array.Sort(properties, (left, right) =>
        {
            var byName = names.AsSpan(left.Start, left.Length).SequenceCompareTo(names.AsSpan(right.Start, right.Length));
            return byName != 0 ? byName : left.Position.CompareTo(right.Position);
        });

    static int CopyName(JsonProperty property, Span<char> destination)
    {
        var raw = JsonMarshal.GetRawUtf8PropertyName(property);
        if (!raw.Contains((byte)'\\')) return Encoding.UTF8.GetChars(raw, destination);

        byte[]? rented = null;
        var quoted = raw.Length <= 254 ? stackalloc byte[raw.Length + 2] : (rented = ArrayPool<byte>.Shared.Rent(raw.Length + 2)).AsSpan();
        try
        {
            quoted[0] = (byte)'"';
            raw.CopyTo(quoted[1..]);
            quoted[raw.Length + 1] = (byte)'"';
            var reader = new Utf8JsonReader(quoted[..(raw.Length + 2)]);
            reader.Read();
            return reader.CopyString(destination);
        }
        finally
        {
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                WriteObject(element, writer);
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

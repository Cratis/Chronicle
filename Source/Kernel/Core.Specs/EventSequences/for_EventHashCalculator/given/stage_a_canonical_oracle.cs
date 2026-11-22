// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

/// <summary>
/// Independent serialize/parse/canonicalize oracle: no production writer, sorter, buffer or hash helper.
/// OrderBy retains Stage A's stable ordering for duplicate decoded property names.
/// </summary>
public static class stage_a_canonical_oracle
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static byte[] Encode(ExpandoObject content)
    {
        var serialized = JsonSerializer.SerializeToUtf8Bytes((object)content, _serializerOptions);
        using var document = JsonDocument.Parse(serialized);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, _writerOptions);
        WriteSorted(document.RootElement, writer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    public static string Hash(EventTypeId eventType, EventSourceId eventSource, byte[] canonicalBytes)
    {
        var prefix = Encoding.UTF8.GetBytes($"{eventType.Value}|{eventSource.Value}|");
        var payload = new byte[prefix.Length + canonicalBytes.Length];
        prefix.CopyTo(payload, 0);
        canonicalBytes.CopyTo(payload, prefix.Length);
        return Convert.ToBase64String(SHA256.HashData(payload));
    }

    static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSorted(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteSorted(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}

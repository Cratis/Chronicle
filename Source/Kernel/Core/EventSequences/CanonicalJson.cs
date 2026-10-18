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

    static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
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

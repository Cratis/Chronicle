// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Canonicalizes payloads using the event hash calculator's ordinal object ordering.
/// </summary>
internal static class OccurrencePayloadFingerprint
{
    /// <summary>
    /// Hashes the trigger and canonical JSON without depending on property insertion order.
    /// </summary>
    /// <param name="trigger">The trigger identity.</param>
    /// <param name="payload">The admitted JSON.</param>
    /// <returns>The SHA-256 fingerprint.</returns>
    internal static string Calculate(string trigger, JsonElement payload)
    {
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write(Encoding.UTF8.GetBytes($"{trigger}|"));
        using var writer = new Utf8JsonWriter(buffer, new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        Write(payload, writer);
        writer.Flush();
        return Convert.ToBase64String(SHA256.HashData(buffer.WrittenSpan));
    }

    static void Write(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(item, writer);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}

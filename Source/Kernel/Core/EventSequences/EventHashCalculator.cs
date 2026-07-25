// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventHashCalculator"/>.
/// </summary>
[Singleton]
public class EventHashCalculator : IEventHashCalculator
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

    /// <inheritdoc/>
    public EventHash Calculate(EventTypeId eventTypeId, EventSourceId eventSourceId, ExpandoObject content)
    {
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write(Encoding.UTF8.GetBytes($"{eventTypeId.Value}|{eventSourceId.Value}|"));
        WriteCanonicalJson(content, buffer);
        var hashBytes = SHA256.HashData(buffer.WrittenSpan);
        return Convert.ToBase64String(hashBytes);
    }

    static void WriteCanonicalJson(ExpandoObject content, IBufferWriter<byte> buffer)
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

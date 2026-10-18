// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Encodes a composite read model key as a single canonical string for sinks that store a key in one column.
/// </summary>
/// <remarks>
/// <para>
/// The encoding is a JSON object whose parts are ordered by name using ordinal comparison. Each value is written by its
/// type using invariant formatting: numbers as JSON numbers (decimals without trailing zeros), Guids in the <c language="csharp">D</c>
/// format, dates and times in the round-trip <c language="csharp">O</c> format, a <see cref="TimeSpan"/> in the constant <c language="csharp">c</c> format,
/// an enum as its underlying number and a concept as the value it holds. A missing value stays JSON <see langword="null"/>, so it is
/// distinct from an empty string, and every string is quoted, so a value cannot contain a separator of another part.
/// </para>
/// <para>
/// The same parts therefore always produce the same string, whatever order the projection mapped them in and whatever
/// culture the kernel runs under, and different parts never produce the same string. The string can be parsed back into
/// the key's parts with <see cref="TryDecode"/>.
/// </para>
/// </remarks>
public static class CompositeKeyEncoding
{
    static readonly JsonWriterOptions _writerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false
    };

    /// <summary>
    /// Encodes the parts of a composite key.
    /// </summary>
    /// <param name="parts">The parts of the key, by name.</param>
    /// <returns>The canonical string for the key.</returns>
    public static string Encode(IDictionary<string, object?> parts)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, _writerOptions))
        {
            WriteObject(writer, parts);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Tries to parse a string produced by <see cref="Encode"/> back into the parts of the key.
    /// </summary>
    /// <param name="encoded">The string to parse.</param>
    /// <param name="parts">The parts of the key as a <see cref="JsonObject"/>, when <paramref name="encoded"/> is an encoded composite key.</param>
    /// <returns>True if <paramref name="encoded"/> is an encoded composite key, false if not.</returns>
    public static bool TryDecode(string? encoded, [NotNullWhen(true)] out JsonObject? parts)
    {
        parts = null;
        if (string.IsNullOrEmpty(encoded) || !encoded.StartsWith('{'))
        {
            return false;
        }

        try
        {
            parts = JsonNode.Parse(encoded) as JsonObject;
        }
        catch (JsonException)
        {
            // Not every string starting with a brace is an encoded key - a legacy key part can start with one.
            return false;
        }

        return parts is not null;
    }

    static void WriteObject(Utf8JsonWriter writer, IDictionary<string, object?> parts)
    {
        writer.WriteStartObject();
        foreach (var (name, value) in parts.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            writer.WritePropertyName(name);
            WriteValue(writer, value);
        }

        writer.WriteEndObject();
    }

    static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        if (value?.IsConcept() == true)
        {
            value = value.GetConceptValue();
        }

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            case Enum enumValue:
                writer.WriteNumberValue(Convert.ToDecimal(enumValue, CultureInfo.InvariantCulture));
                break;
            case IDictionary<string, object?> nested:
                WriteObject(writer, nested);
                break;
            default:
                WriteScalar(writer, value);
                break;
        }
    }

    static void WriteScalar(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case int or short or sbyte or byte or ushort:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case uint unsigned:
                writer.WriteNumberValue(unsigned);
                break;
            case long signed:
                writer.WriteNumberValue(signed);
                break;
            case ulong unsigned:
                writer.WriteNumberValue(unsigned);
                break;
            case decimal number:
                // Dividing by one with a large scale drops the trailing zeros, so 1.0 and 1.00 address the same key.
                writer.WriteNumberValue(number / 1.000000000000000000000000000000000m);
                break;
            case double number when double.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case float number when float.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case double or float:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            default:
                writer.WriteStringValue(FormatAsText(value));
                break;
        }
    }

    static string FormatAsText(object value) => value switch
    {
        Guid guid => guid.ToString("D"),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}

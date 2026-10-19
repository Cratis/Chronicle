// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Encodes a composite read model key as a single compact string for sinks that store a key in one column.
/// </summary>
/// <remarks>
/// <para>
/// The encoding is the <see cref="Prefix"/> marker followed by the parts ordered by name using ordinal comparison. Part names are
/// not written - the key schema of the read model names them. Each part is <c language="csharp">&lt;length&gt;:&lt;value&gt;</c>, where the
/// length is the number of characters of the value, or a single <c language="csharp">-</c> for a missing value. A value is formatted by its type using
/// invariant formatting: numbers without trailing zeros, Guids in the <c language="csharp">D</c> format, dates and times in the round-trip
/// <c language="csharp">O</c> format, a <see cref="TimeSpan"/> in the constant <c language="csharp">c</c> format, an enum as its
/// underlying number and a concept as the value it holds.
/// </para>
/// <para>
/// Because every value carries its length, no value can be mistaken for part of another, a missing value is distinct from an empty
/// string, and the same parts always produce the same string whatever order the projection mapped them in and whatever culture the
/// kernel runs under. The string can be parsed back into the parts with <see cref="TryDecode"/> given the key schema.
/// </para>
/// </remarks>
public static class CompositeKeyEncoding
{
    /// <summary>
    /// The marker an encoded key starts with. The legacy underscore-joined ids never started with it.
    /// </summary>
    public const string Prefix = "~k1:";

    const string NullToken = "-";

    /// <summary>
    /// Encodes the parts of a composite key.
    /// </summary>
    /// <param name="parts">The parts of the key, by name.</param>
    /// <returns>The encoded string for the key.</returns>
    public static string Encode(IDictionary<string, object?> parts)
    {
        var builder = new StringBuilder(Prefix);
        AppendParts(builder, parts);
        return builder.ToString();
    }

    /// <summary>
    /// Determines whether an id was produced by <see cref="Encode"/>.
    /// </summary>
    /// <param name="id">The stored id.</param>
    /// <returns>True if the id is an encoded composite key, false if not.</returns>
    public static bool IsEncoded(string? id) => id?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// Tries to parse a string produced by <see cref="Encode"/> back into the parts of the key.
    /// </summary>
    /// <param name="encoded">The string to parse.</param>
    /// <param name="partKinds">The kind of every part of the key by name, from the key schema of the read model.</param>
    /// <param name="parts">The parts of the key, when <paramref name="encoded"/> is an encoded composite key of those parts.</param>
    /// <returns>True if <paramref name="encoded"/> is an encoded composite key of the given parts, false if not.</returns>
    public static bool TryDecode(string? encoded, IReadOnlyDictionary<string, CompositeKeyPartKind> partKinds, [NotNullWhen(true)] out JsonObject? parts)
    {
        parts = null;
        if (!IsEncoded(encoded))
        {
            return false;
        }

        var result = new JsonObject();
        var position = Prefix.Length;
        foreach (var (name, kind) in partKinds.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            if (!TryReadPart(encoded!, ref position, out var value))
            {
                return false;
            }

            result[name] = value is null ? null : ToNode(value, kind);
        }

        if (position != encoded!.Length)
        {
            return false;
        }

        parts = result;
        return true;
    }

    static bool TryReadPart(string encoded, ref int position, out string? value)
    {
        value = null;
        if (position >= encoded.Length)
        {
            return false;
        }

        if (encoded[position] == NullToken[0])
        {
            position++;
            return true;
        }

        var separator = encoded.IndexOf(':', position);
        if (separator < 0 || !int.TryParse(encoded.AsSpan(position, separator - position), NumberStyles.None, CultureInfo.InvariantCulture, out var length) || separator + 1 + length > encoded.Length)
        {
            return false;
        }

        value = encoded.Substring(separator + 1, length);
        position = separator + 1 + length;
        return true;
    }

    static JsonValue ToNode(string value, CompositeKeyPartKind kind)
    {
        switch (kind)
        {
            case CompositeKeyPartKind.WholeNumber when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer):
                return JsonValue.Create(integer);
            case CompositeKeyPartKind.WholeNumber when ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsigned):
                return JsonValue.Create(unsigned);
            case CompositeKeyPartKind.Number when decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number):
                return JsonValue.Create(number);
            case CompositeKeyPartKind.Number when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floating):
                return JsonValue.Create(floating);
            case CompositeKeyPartKind.Boolean when bool.TryParse(value, out var boolean):
                return JsonValue.Create(boolean);
            default:
                return JsonValue.Create(value);
        }
    }

    static void AppendParts(StringBuilder builder, IDictionary<string, object?> parts)
    {
        foreach (var (_, value) in parts.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var text = FormatValue(value);
            if (text is null)
            {
                builder.Append(NullToken[0]);
            }
            else
            {
                builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
            }
        }
    }

    static string? FormatValue(object? value)
    {
        if (value?.IsConcept() == true)
        {
            value = value.GetConceptValue();
        }

        return value switch
        {
            null => null,
            string text => text,
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => Convert.ToDecimal(enumValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IDictionary<string, object?> nested => EncodeNested(nested),
            decimal number => (number / 1.000000000000000000000000000000000m).ToString(CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            _ => FormatAsText(value)
        };
    }

    static string EncodeNested(IDictionary<string, object?> nested)
    {
        var builder = new StringBuilder();
        AppendParts(builder, nested);
        return builder.ToString();
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

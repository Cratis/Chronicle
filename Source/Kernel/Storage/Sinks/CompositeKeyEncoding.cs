// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Encodes the parts of a composite read model key as one canonical string.
/// </summary>
/// <remarks>
/// <para>
/// The encoding is the <see cref="Prefix"/> marker followed by every part, ordered by part name using ordinal comparison.
/// Part names are not written; the key schema of the read model names them. A part is <c language="csharp">&lt;length&gt;:&lt;value&gt;</c>, where the
/// length is the number of characters of the value, or a single <c language="csharp">-</c> when the value is missing or null.
/// </para>
/// <para>
/// Values are formatted using invariant formatting: decimals without trailing zeros, Guids in the <c language="csharp">D</c> format, dates and times
/// in the round-trip <c language="csharp">O</c> format (<see cref="DateTime"/> and <see cref="DateTimeOffset"/> normalized to UTC), enums as their
/// underlying number, booleans as <see langword="true"/> or <see langword="false"/> and concepts as the value they hold. Nested objects are refused.
/// </para>
/// <para>
/// Because every value carries its length the encoding is injective: a separator inside a value cannot collide with another key, and a
/// null part is distinct from an empty one. The result does not depend on the order the parts are given in or on the culture of the process.
/// </para>
/// </remarks>
public static class CompositeKeyEncoding
{
    /// <summary>
    /// The marker, and version, every encoded key starts with. A future encoding uses a different marker.
    /// </summary>
    public const string Prefix = "~k1:";

    const char NullToken = '-';
    const char LegacySeparator = '_';

    /// <summary>
    /// Encodes the parts of a composite key.
    /// </summary>
    /// <param name="parts">The parts of the key by name.</param>
    /// <returns>The canonical encoded key.</returns>
    /// <exception cref="NestedCompositeKeyPartsAreNotSupported">A part holds a nested object.</exception>
    public static string Encode(IEnumerable<KeyValuePair<string, object?>> parts)
    {
        var builder = new StringBuilder(Prefix);
        foreach (var (name, value) in parts.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var text = Format(name, value);
            if (text is null)
            {
                builder.Append(NullToken);
                continue;
            }

            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether an id was produced by <see cref="Encode"/>.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns>True if the id starts with the <see cref="Prefix"/> marker, false if not.</returns>
    public static bool IsEncoded(string? id) => id?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// Determines whether the legacy, underscore-joined id of a key can collide with the id of another key.
    /// </summary>
    /// <param name="parts">The parts of the key by name.</param>
    /// <returns>True if any part contains an underscore or is null or empty, false if not.</returns>
    public static bool IsLegacyIdAmbiguous(IEnumerable<KeyValuePair<string, object?>> parts) =>
        parts.Any(part =>
        {
            var text = part.Value?.ToString();
            return string.IsNullOrEmpty(text) || text.Contains(LegacySeparator);
        });

    static DateTime AsUtc(DateTime dateTime) => dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime();

    static string? Format(string name, object? value)
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
            decimal number => (number / 1.000000000000000000000000000000000m).ToString(CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => AsUtc(dateTime).ToString("O", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
            IDictionary<string, object?> => throw new NestedCompositeKeyPartsAreNotSupported(name),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}

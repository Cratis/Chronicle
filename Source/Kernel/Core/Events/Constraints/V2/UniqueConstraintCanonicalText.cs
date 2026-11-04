// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Implements the CLR canonical forms of decision 0019, without normalizing Unicode.
/// </summary>
internal static class UniqueConstraintCanonicalText
{
    const string UtcInstant = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    static readonly Dictionary<string, Func<object, string>> _formats = new(StringComparer.Ordinal)
    {
        ["guid"] = value => ((Guid)value).ToString("D").ToLowerInvariant(),
        ["uuid"] = value => ((Guid)value).ToString("D").ToLowerInvariant(),
        ["int16"] = UniqueConstraintCanonicalNumber.Integer,
        ["int32"] = UniqueConstraintCanonicalNumber.Integer,
        ["int64"] = UniqueConstraintCanonicalNumber.Integer,
        ["byte"] = UniqueConstraintCanonicalNumber.Integer,
        ["uint32"] = UniqueConstraintCanonicalNumber.Integer,
        ["uint64"] = UniqueConstraintCanonicalNumber.Integer,
        ["float"] = value => UniqueConstraintCanonicalNumber.Single((float)value),
        ["double"] = value => UniqueConstraintCanonicalNumber.Double((double)value),
        ["decimal"] = value => UniqueConstraintCanonicalNumber.Decimal((decimal)value),
        ["date-time"] = value => AsUtc((DateTime)value).ToString(UtcInstant, CultureInfo.InvariantCulture),
        ["date-time-offset"] = value => ((DateTimeOffset)value).UtcDateTime.ToString(UtcInstant, CultureInfo.InvariantCulture),
        ["date"] = value => ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["time"] = value => ((TimeOnly)value).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        ["duration"] = value => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Rejects formats that cannot participate in a v2 constraint, even when their value is null.
    /// </summary>
    /// <param name="format">The schema format.</param>
    /// <exception cref="UnsupportedUniqueConstraintFormat">The format has no v2 representation.</exception>
    internal static void ValidateFormat(string? format)
    {
        if (!string.IsNullOrEmpty(format) && !_formats.ContainsKey(format))
        {
            throw new UnsupportedUniqueConstraintFormat(format);
        }
    }

    /// <summary>
    /// Canonicalizes one non-null scalar. Collection framing is handled separately.
    /// </summary>
    /// <param name="value">The schema-typed CLR value.</param>
    /// <param name="format">The schema scalar format.</param>
    /// <param name="isInSchema">Whether the property is declared in the schema.</param>
    /// <param name="ignoreCasing">Whether string leaves are lowercased.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">The format or scalar kind is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">The number is not finite.</exception>
    internal static string For(object value, string? format, bool isInSchema, bool ignoreCasing)
    {
        ValidateFormat(format);
        value = ApplyProviderPrecision(value);
        if (!string.IsNullOrEmpty(format))
        {
            return _formats[format](value);
        }
        return WithoutFormat(value, isInSchema, ignoreCasing);
    }

    static string WithoutFormat(object value, bool isInSchema, bool ignoreCasing) => value switch
    {
        string text => String(text, isInSchema, ignoreCasing),
        bool boolean => boolean ? "true" : "false",
        sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal =>
            UniqueConstraintCanonicalNumber.Double(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
        _ => throw new UnsupportedUniqueConstraintFormat(value.GetType().Name)
    };

    static string String(string text, bool isInSchema, bool ignoreCasing)
    {
        if (!isInSchema && Guid.TryParse(text, out var guid))
        {
            return guid.ToString("D").ToLowerInvariant();
        }
        return ignoreCasing ? text.ToLowerInvariant() : text;
    }

    static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime();

    /// <summary>
    /// Applies provider precision, which is UNVERIFIED. This is the single future truncation point;
    /// until provider round-trip vectors establish a common precision, the CLR value is preserved exactly.
    /// </summary>
    /// <param name="value">The CLR value.</param>
    /// <returns>The value, unchanged.</returns>
    static object ApplyProviderPrecision(object value) => value;
}

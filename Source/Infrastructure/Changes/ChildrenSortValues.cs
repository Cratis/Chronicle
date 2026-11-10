// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents the deterministic, culture-free total order children are sorted by.
/// </summary>
/// <remarks>
/// <para>
/// The order matches how MongoDB compares the stored values, so the engine and every sink store children in the same order,
/// on any host culture. Values of different kinds are ordered by kind the way BSON orders types: missing and null first,
/// then numbers, strings, objects, arrays, binary data (Guid), booleans and finally dates.
/// </para>
/// <para>
/// Within a kind: numbers compare numerically across numeric types, strings compare by Unicode code point - the order of
/// their UTF-8 bytes, which is how MongoDB compares them, not the order of their UTF-16 code units - Guids compare by their
/// big-endian (RFC 4122) bytes - how Chronicle stores them with the standard Guid representation - booleans order
/// false before true, and dates compare as UTC instants at the millisecond precision they are stored with.
/// </para>
/// </remarks>
public static class ChildrenSortValues
{
    enum Kind
    {
        Null = 0,
        Number = 1,
        String = 2,
        Object = 3,
        Array = 4,
        Binary = 5,
        Boolean = 6,
        Date = 7
    }

    /// <summary>
    /// Compare two sort values.
    /// </summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns>Less than zero when left comes before right, zero when equal and greater than zero when after.</returns>
    public static int Compare(object? left, object? right)
    {
        left = Normalize(left);
        right = Normalize(right);

        var leftKind = KindOf(left);
        var rightKind = KindOf(right);
        if (leftKind != rightKind)
        {
            return leftKind.CompareTo(rightKind);
        }

        return leftKind switch
        {
            Kind.Number => CompareNumbers(left!, right!),
            Kind.String => CompareByCodePoint((string)left!, (string)right!),
            Kind.Binary => ((Guid)left!).ToByteArray(bigEndian: true).AsSpan().SequenceCompareTo(((Guid)right!).ToByteArray(bigEndian: true)),
            Kind.Boolean => ((bool)left!).CompareTo((bool)right!),
            Kind.Date => ((UtcMilliseconds)left!).Value.CompareTo(((UtcMilliseconds)right!).Value),
            _ => 0
        };
    }

    static object? Normalize(object? value) => value switch
    {
        null => null,
        Enum enumValue => Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => new UtcMilliseconds(dateTimeOffset.ToUnixTimeMilliseconds()),
        DateTime dateTime => new UtcMilliseconds(ToUnixTimeMilliseconds(dateTime)),
        DateOnly dateOnly => new UtcMilliseconds(ToUnixTimeMilliseconds(dateOnly.ToDateTime(new TimeOnly(12, 0)))),
        TimeOnly timeOnly => new UtcMilliseconds(ToUnixTimeMilliseconds(DateTime.UnixEpoch + timeOnly.ToTimeSpan())),
        char character => character.ToString(),
        TimeSpan timeSpan => timeSpan.ToString(),
        Uri uri => uri.ToString(),
        Type type => type.AssemblyQualifiedName,
        _ => value
    };

    static Kind KindOf(object? value) => value switch
    {
        null => Kind.Null,
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Kind.Number,
        string => Kind.String,
        Guid => Kind.Binary,
        bool => Kind.Boolean,
        UtcMilliseconds => Kind.Date,
        IDictionary => Kind.Object,
        IDictionary<string, object?> => Kind.Object,
        IEnumerable => Kind.Array,
        _ => Kind.Object
    };

    static int CompareNumbers(object left, object right)
    {
        if (left is float or double || right is float or double)
        {
            return Convert.ToDouble(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(right, CultureInfo.InvariantCulture));
        }

        return Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(right, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Compare two strings by Unicode code point, which is the order of their UTF-8 bytes.
    /// </summary>
    /// <remarks>
    /// UTF-16 code unit order differs from code point order only where a surrogate meets a code unit in U+E000-U+FFFF:
    /// the surrogate encodes a code point above U+FFFF and so must sort after it. Shifting the code units at the first
    /// difference fixes that without decoding or allocating.
    /// </remarks>
    /// <param name="left">Left string.</param>
    /// <param name="right">Right string.</param>
    /// <returns>-1, 0 or 1.</returns>
    static int CompareByCodePoint(string left, string right)
    {
        var index = left.AsSpan().CommonPrefixLength(right);
        if (index == left.Length || index == right.Length)
        {
            return left.Length.CompareTo(right.Length);
        }

        return Math.Sign(CodePointOrderOf(left[index]) - CodePointOrderOf(right[index]));
    }

    static int CodePointOrderOf(char codeUnit) => codeUnit switch
    {
        < '\uD800' => codeUnit,
        >= '\uE000' => codeUnit - 0x800,
        _ => codeUnit + 0x2000
    };

    static long ToUnixTimeMilliseconds(DateTime dateTime) =>
        new DateTimeOffset(dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();

    /// <summary>
    /// A date normalized to milliseconds since the Unix epoch in UTC.
    /// </summary>
    /// <param name="Value">The milliseconds.</param>
    sealed record UtcMilliseconds(long Value);
}

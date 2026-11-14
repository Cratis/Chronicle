// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Remembers the UTC offset a date-time value was written with.
/// </summary>
/// <remarks>
/// A <see cref="DateTime"/> has no offset, and the expando objects of the kernel hold <see cref="DateTime"/> for a
/// date-time value, so reading a value written with an offset would otherwise write it back with the offset of the host. The offset is kept against the boxed value, which is the instance the expando object holds. A value that was
/// copied or computed has no remembered offset and is written as it always was.
/// </remarks>
static class OriginalOffsets
{
    static readonly ConditionalWeakTable<object, object> _offsets = new();

    /// <summary>
    /// Boxes a value read from JSON and remembers its offset when it has one that the <see cref="DateTime"/> cannot express.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> as read from JSON.</param>
    /// <param name="offset">The <see cref="DateTimeOffset"/> as read from the same JSON.</param>
    /// <returns>The boxed <paramref name="dateTime"/>.</returns>
    internal static object Remember(DateTime dateTime, DateTimeOffset offset)
    {
        object boxed = dateTime;

        // A UTC value is written as UTC and one without an offset has none to keep; only an offset is converted to local time.
        if (dateTime.Kind == DateTimeKind.Local)
        {
            _offsets.AddOrUpdate(boxed, offset.Offset);
        }

        return boxed;
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> from a JSON string, remembering its offset.
    /// </summary>
    /// <param name="text">The ISO 8601 text of the value.</param>
    /// <param name="dateTime">The <see cref="DateTime"/> that was read.</param>
    /// <returns>The boxed <paramref name="dateTime"/>.</returns>
    internal static object Remember(string text, DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Local && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var offset)
            ? Remember(dateTime, offset)
            : dateTime;

    /// <summary>
    /// Creates the JSON value of a <see cref="DateTime"/> that was read with an offset, using that offset.
    /// </summary>
    /// <param name="value">The value held by the expando object.</param>
    /// <param name="jsonValue">The <see cref="JsonValue"/>, when the value remembers an offset.</param>
    /// <returns>True when the value remembers an offset.</returns>
    internal static bool TryCreateJsonValue(object value, out JsonValue? jsonValue)
    {
        jsonValue = null;
        if (value is DateTime { Kind: DateTimeKind.Local } dateTime && _offsets.TryGetValue(value, out var offset))
        {
            jsonValue = JsonValue.Create(new DateTimeOffset(dateTime).ToOffset((TimeSpan)offset));
            return true;
        }

        return false;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Extensions for date and time types.
/// </summary>
public static class DateAndTimeExtensions
{
    /// <summary>
    /// Rounds down the date and time to the nearest 10 milliseconds.
    /// </summary>
    /// <param name="dateTime">The date and time to round down.</param>
    /// <returns>The rounded down date and time.</returns>
    public static DateTimeOffset RoundDownTicks(this DateTimeOffset dateTime)
    {
        var ticks = dateTime.Ticks / 10000 * 10000;
        return new DateTimeOffset(ticks, dateTime.Offset);
    }

    /// <summary>
    /// Rounds down the date and time to the nearest 10 milliseconds.
    /// </summary>
    /// <param name="dateTime">The date and time to round down.</param>
    /// <returns>The rounded down date and time.</returns>
    public static DateTime RoundDownTicks(this DateTime dateTime)
    {
        var ticks = dateTime.Ticks / 10000 * 10000;
        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Rounds down the date and time to the nearest 10 milliseconds.
    /// </summary>
    /// <param name="time">The time to round down.</param>
    /// <returns>The rounded down time.</returns>
    public static TimeOnly RoundDownTicks(this TimeOnly time)
    {
        var ticks = time.Ticks / 10000 * 10000;
        return new TimeOnly(ticks);
    }
}

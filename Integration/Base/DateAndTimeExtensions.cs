// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Base;

public static class DateAndTimeExtensions
{
    public static DateTimeOffset RoundDownTicks(this DateTimeOffset dateTime)
    {
        var ticks = dateTime.Ticks / 10000 * 10000;
        return new DateTimeOffset(ticks, dateTime.Offset);
    }

    public static DateTime RoundDownTicks(this DateTime dateTime)
    {
        var ticks = dateTime.Ticks / 10000 * 10000;
        return new DateTime(ticks, dateTime.Kind);
    }

    public static TimeOnly RoundDownTicks(this TimeOnly time)
    {
        var ticks = time.Ticks / 10000 * 10000;
        return new TimeOnly(ticks);
    }
}

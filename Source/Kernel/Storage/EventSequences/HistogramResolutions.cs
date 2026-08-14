// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Provides bucketing helpers for <see cref="HistogramResolution"/>.
/// </summary>
/// <remarks>
/// Storage providers that cannot express date truncation natively use these helpers so every
/// backend buckets a timestamp to exactly the same instant.
/// </remarks>
public static class HistogramResolutions
{
    /// <summary>
    /// Truncate a timestamp down to the start of the bucket it belongs to.
    /// </summary>
    /// <param name="occurred">The <see cref="DateTimeOffset"/> to truncate.</param>
    /// <param name="resolution">The <see cref="HistogramResolution"/> defining the bucket size.</param>
    /// <returns>The inclusive start of the bucket, in UTC.</returns>
    public static DateTimeOffset Truncate(DateTimeOffset occurred, HistogramResolution resolution)
    {
        var utc = occurred.ToUniversalTime();

        return resolution switch
        {
            HistogramResolution.Minute => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero),
            HistogramResolution.Hour => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero),
            HistogramResolution.Day => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            HistogramResolution.Week => StartOfWeek(utc),
            HistogramResolution.Month => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
            _ => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero)
        };
    }

    /// <summary>
    /// Pick the resolution that keeps a time span within a reasonable number of buckets.
    /// </summary>
    /// <param name="span">The <see cref="TimeSpan"/> the histogram covers.</param>
    /// <returns>The <see cref="HistogramResolution"/> to use.</returns>
    public static HistogramResolution ForSpan(TimeSpan span) => span switch
    {
        _ when span <= TimeSpan.FromHours(2) => HistogramResolution.Minute,
        _ when span <= TimeSpan.FromDays(4) => HistogramResolution.Hour,
        _ when span <= TimeSpan.FromDays(90) => HistogramResolution.Day,
        _ when span <= TimeSpan.FromDays(730) => HistogramResolution.Week,
        _ => HistogramResolution.Month
    };

    /// <summary>
    /// Monday is treated as the first day of the week, matching MongoDB's default for date truncation.
    /// </summary>
    /// <param name="utc">The UTC timestamp to find the containing week for.</param>
    /// <returns>Midnight on the Monday of the week the timestamp belongs to.</returns>
    static DateTimeOffset StartOfWeek(DateTimeOffset utc)
    {
        var day = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero);
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;

        return day.AddDays(-daysSinceMonday);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_HistogramResolutions;

public class when_truncating_a_timestamp
{
    /// <summary>
    /// A Wednesday, so the week bucket has to move back two days to reach Monday.
    /// </summary>
    static readonly DateTimeOffset _occurred = new(2026, 8, 12, 14, 37, 42, 815, TimeSpan.Zero);

    [Fact] void should_keep_the_minute_for_minute_resolution() =>
        HistogramResolutions.Truncate(_occurred, HistogramResolution.Minute)
            .ShouldEqual(new DateTimeOffset(2026, 8, 12, 14, 37, 0, TimeSpan.Zero));

    [Fact] void should_keep_the_hour_for_hour_resolution() =>
        HistogramResolutions.Truncate(_occurred, HistogramResolution.Hour)
            .ShouldEqual(new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero));

    [Fact] void should_keep_the_day_for_day_resolution() =>
        HistogramResolutions.Truncate(_occurred, HistogramResolution.Day)
            .ShouldEqual(new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero));

    [Fact] void should_move_back_to_monday_for_week_resolution() =>
        HistogramResolutions.Truncate(_occurred, HistogramResolution.Week)
            .ShouldEqual(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));

    [Fact] void should_keep_the_month_for_month_resolution() =>
        HistogramResolutions.Truncate(_occurred, HistogramResolution.Month)
            .ShouldEqual(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact] void should_normalize_an_offset_timestamp_to_utc_before_bucketing() =>
        HistogramResolutions.Truncate(new DateTimeOffset(2026, 8, 12, 1, 30, 0, TimeSpan.FromHours(2)), HistogramResolution.Day)
            .ShouldEqual(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero));

    [Fact] void should_stay_on_monday_when_the_timestamp_is_already_a_monday() =>
        HistogramResolutions.Truncate(new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero), HistogramResolution.Week)
            .ShouldEqual(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));

    [Fact] void should_move_a_sunday_back_a_full_week() =>
        HistogramResolutions.Truncate(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero), HistogramResolution.Week)
            .ShouldEqual(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));
}

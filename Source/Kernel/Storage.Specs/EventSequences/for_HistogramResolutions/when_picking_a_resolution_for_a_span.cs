// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_HistogramResolutions;

public class when_picking_a_resolution_for_a_span
{
    [Fact] void should_pick_minutes_for_an_hour() =>
        HistogramResolutions.ForSpan(TimeSpan.FromHours(1)).ShouldEqual(HistogramResolution.Minute);

    [Fact] void should_pick_hours_for_a_day() =>
        HistogramResolutions.ForSpan(TimeSpan.FromDays(1)).ShouldEqual(HistogramResolution.Hour);

    [Fact] void should_pick_days_for_a_month() =>
        HistogramResolutions.ForSpan(TimeSpan.FromDays(30)).ShouldEqual(HistogramResolution.Day);

    [Fact] void should_pick_weeks_for_a_year() =>
        HistogramResolutions.ForSpan(TimeSpan.FromDays(365)).ShouldEqual(HistogramResolution.Week);

    [Fact] void should_pick_months_for_a_decade() =>
        HistogramResolutions.ForSpan(TimeSpan.FromDays(3650)).ShouldEqual(HistogramResolution.Month);

    [Fact] void should_pick_minutes_for_an_empty_span() =>
        HistogramResolutions.ForSpan(TimeSpan.Zero).ShouldEqual(HistogramResolution.Minute);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

/// <summary>
/// Time facets come from the event's own occurred timestamp, never from the clock at processing time. A seeded
/// history and a replay both have to land in the buckets the events actually belong to, or the mined patterns
/// describe when the store was loaded rather than when the behavior happened.
/// </summary>
public class from_a_backdated_event : given.an_extractor
{
    static readonly DateTimeOffset _backdated = new(2024, 3, 6, 20, 45, 0, TimeSpan.Zero);

    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(occurred: _backdated));

    [Fact] void should_take_the_year_from_the_event() => _result.Year.ShouldEqual(2024);
    [Fact] void should_take_the_month_from_the_event() => _result.Month.ShouldEqual(3);
    [Fact] void should_take_the_day_of_week_from_the_event() => _result.Day.ShouldEqual(DayOfWeek.Wednesday);
    [Fact] void should_take_the_time_bucket_from_the_event() => _result.TimeBucket.ShouldEqual(TimeBucket.Evening);
    [Fact] void should_carry_the_occurred_timestamp() => _result.Occurred.ShouldEqual(_backdated);
}

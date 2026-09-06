// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_TimeBucketExtensions.when_bucketing_a_moment;

public class moments_across_a_day : Specification
{
    static DateTimeOffset At(int hour) => new(2026, 8, 24, hour, 30, 0, TimeSpan.Zero);

    [Fact] void should_put_the_small_hours_at_night() => At(2).ToTimeBucket().ShouldEqual(TimeBucket.Night);
    [Fact] void should_end_the_night_just_before_five() => At(4).ToTimeBucket().ShouldEqual(TimeBucket.Night);
    [Fact] void should_start_the_early_morning_at_five() => At(5).ToTimeBucket().ShouldEqual(TimeBucket.EarlyMorning);
    [Fact] void should_put_nine_in_the_morning() => At(9).ToTimeBucket().ShouldEqual(TimeBucket.Morning);
    [Fact] void should_put_twelve_at_midday() => At(12).ToTimeBucket().ShouldEqual(TimeBucket.Midday);
    [Fact] void should_put_fifteen_in_the_afternoon() => At(15).ToTimeBucket().ShouldEqual(TimeBucket.Afternoon);
    [Fact] void should_put_nineteen_in_the_evening() => At(19).ToTimeBucket().ShouldEqual(TimeBucket.Evening);
    [Fact] void should_put_twenty_three_at_night() => At(23).ToTimeBucket().ShouldEqual(TimeBucket.Night);
}

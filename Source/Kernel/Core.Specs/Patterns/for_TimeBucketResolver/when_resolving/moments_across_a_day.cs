// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_TimeBucketResolver.when_resolving;

public class moments_across_a_day : Specification
{
    TimeBucketResolver _resolver;

    void Establish() => _resolver = new();

    static DateTimeOffset At(int hour) => new(2026, 8, 24, hour, 30, 0, TimeSpan.Zero);

    [Fact] void should_put_six_in_the_early_morning() => _resolver.Resolve(At(6)).ShouldEqual(TimeBucket.EarlyMorning);
    [Fact] void should_put_nine_in_the_morning() => _resolver.Resolve(At(9)).ShouldEqual(TimeBucket.Morning);
    [Fact] void should_put_twelve_at_midday() => _resolver.Resolve(At(12)).ShouldEqual(TimeBucket.Midday);
    [Fact] void should_put_fifteen_in_the_afternoon() => _resolver.Resolve(At(15)).ShouldEqual(TimeBucket.Afternoon);
    [Fact] void should_put_nineteen_in_the_evening() => _resolver.Resolve(At(19)).ShouldEqual(TimeBucket.Evening);
    [Fact] void should_put_twenty_three_at_night() => _resolver.Resolve(At(23)).ShouldEqual(TimeBucket.Night);
    [Fact] void should_put_the_small_hours_at_night() => _resolver.Resolve(At(2)).ShouldEqual(TimeBucket.Night);
    [Fact] void should_start_the_early_morning_at_five() => _resolver.Resolve(At(5)).ShouldEqual(TimeBucket.EarlyMorning);
    [Fact] void should_end_the_night_just_before_five() => _resolver.Resolve(At(4)).ShouldEqual(TimeBucket.Night);
}

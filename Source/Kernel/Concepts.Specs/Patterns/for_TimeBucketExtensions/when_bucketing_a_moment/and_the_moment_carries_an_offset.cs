// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_TimeBucketExtensions.when_bucketing_a_moment;

/// <summary>
/// Somebody who works at nine in the morning does so at nine in their own morning. Normalizing to UTC first would
/// scatter that one behavior across buckets as they travel or as daylight saving shifts.
/// <para>
/// The two moments below are the same instant written from two places: nine in the morning in one, three in the
/// morning in the other.
/// </para>
/// </summary>
public class and_the_moment_carries_an_offset : Specification
{
    static readonly DateTimeOffset _ninePlusSix = new(2026, 8, 24, 9, 30, 0, TimeSpan.FromHours(6));
    static readonly DateTimeOffset _threeInUtc = new(2026, 8, 24, 3, 30, 0, TimeSpan.Zero);

    [Fact] void should_read_the_hour_the_offset_gives() => _ninePlusSix.ToTimeBucket().ShouldEqual(TimeBucket.Morning);
    [Fact] void should_bucket_the_same_instant_differently_elsewhere() => _threeInUtc.ToTimeBucket().ShouldEqual(TimeBucket.Night);
    [Fact] void should_be_the_same_instant() => _ninePlusSix.UtcDateTime.ShouldEqual(_threeInUtc.UtcDateTime);
}

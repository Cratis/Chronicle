// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_TimeBucketResolver.when_resolving;

/// <summary>
/// Nine in the morning in Oslo is a morning, not the small hours of the preceding UTC day. Normalizing to UTC
/// first would scatter one person's routine across buckets as their offset changed.
/// </summary>
public class a_moment_in_a_non_utc_offset : Specification
{
    TimeBucketResolver _resolver;
    TimeBucket _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve(new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.FromHours(2)));

    [Fact] void should_resolve_from_the_local_hour() => _result.ShouldEqual(TimeBucket.Morning);
}

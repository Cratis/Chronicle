// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Extension methods for working out the <see cref="TimeBucket"/> a moment falls in.
/// </summary>
/// <remarks>
/// This lives with the concept rather than beside the miner because both sides of the wire need it and need to
/// agree. The engine buckets an event's <c>Occurred</c> when it mines; anything asking what usually happens at a
/// given moment has to bucket that moment the same way, or it asks about a slot the mining never used. A caller
/// left to write the branching itself gets one chance to write it differently, and the mismatch is silent -
/// the query simply returns nothing.
/// </remarks>
public static class TimeBucketExtensions
{
    /// <summary>
    /// Gets the <see cref="TimeBucket"/> a moment falls in.
    /// </summary>
    /// <param name="moment">The moment to bucket.</param>
    /// <returns>The <see cref="TimeBucket"/>.</returns>
    /// <remarks>
    /// Read from the offset the moment carries, not from UTC. Somebody who works at nine in the morning does so at
    /// nine in their own morning, and normalizing to UTC first would scatter that one behavior across buckets as
    /// they travel or as daylight saving shifts.
    /// </remarks>
    public static TimeBucket ToTimeBucket(this DateTimeOffset moment) => moment.Hour switch
    {
        >= 5 and < 8 => TimeBucket.EarlyMorning,
        >= 8 and < 11 => TimeBucket.Morning,
        >= 11 and < 14 => TimeBucket.Midday,
        >= 14 and < 17 => TimeBucket.Afternoon,
        >= 17 and < 22 => TimeBucket.Evening,
        _ => TimeBucket.Night
    };
}

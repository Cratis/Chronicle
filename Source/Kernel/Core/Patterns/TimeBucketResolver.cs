// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="ITimeBucketResolver"/>.
/// </summary>
/// <remarks>
/// Buckets are resolved from the offset the event was appended with, not from UTC. A person who approves expenses
/// at nine in the morning does so at nine in their own morning, and normalizing to UTC first would scatter that one
/// behavior across buckets as they travel or as daylight saving shifts.
/// </remarks>
[Singleton]
public class TimeBucketResolver : ITimeBucketResolver
{
    /// <inheritdoc/>
    public TimeBucket Resolve(DateTimeOffset occurred) => occurred.Hour switch
    {
        >= 5 and < 8 => TimeBucket.EarlyMorning,
        >= 8 and < 11 => TimeBucket.Morning,
        >= 11 and < 14 => TimeBucket.Midday,
        >= 14 and < 17 => TimeBucket.Afternoon,
        >= 17 and < 22 => TimeBucket.Evening,
        _ => TimeBucket.Night
    };
}

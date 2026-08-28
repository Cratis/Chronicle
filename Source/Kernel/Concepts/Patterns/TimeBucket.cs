// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the part of the day an event occurred in.
/// </summary>
/// <remarks>
/// Buckets rather than hours: "approves expenses on Monday mornings" is a pattern a person recognizes, while
/// "approves expenses on Mondays at 09" splits the same behavior across as many patterns as there are hours it
/// happens to land on, and none of them clears a support threshold.
/// </remarks>
public enum TimeBucket
{
    /// <summary>
    /// From 05:00 up to 08:00.
    /// </summary>
    EarlyMorning = 0,

    /// <summary>
    /// From 08:00 up to 11:00.
    /// </summary>
    Morning = 1,

    /// <summary>
    /// From 11:00 up to 14:00.
    /// </summary>
    Midday = 2,

    /// <summary>
    /// From 14:00 up to 17:00.
    /// </summary>
    Afternoon = 3,

    /// <summary>
    /// From 17:00 up to 22:00.
    /// </summary>
    Evening = 4,

    /// <summary>
    /// From 22:00 up to 05:00.
    /// </summary>
    Night = 5
}

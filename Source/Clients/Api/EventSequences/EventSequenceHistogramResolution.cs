// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the time bucket resolution for an event sequence histogram.
/// </summary>
public enum EventSequenceHistogramResolution
{
    /// <summary>
    /// Group events per minute.
    /// </summary>
    Minute = 0,

    /// <summary>
    /// Group events per hour.
    /// </summary>
    Hour = 1,

    /// <summary>
    /// Group events per day.
    /// </summary>
    Day = 2,

    /// <summary>
    /// Group events per week.
    /// </summary>
    Week = 3,

    /// <summary>
    /// Group events per month.
    /// </summary>
    Month = 4
}

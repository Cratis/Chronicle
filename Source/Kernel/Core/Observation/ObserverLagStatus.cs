// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what a measurement of an observer's progress says about it.
/// </summary>
public enum ObserverLagStatus
{
    /// <summary>
    /// Nothing can be said. The zero value on purpose: a missing measurement is never a healthy one, and never a distance of zero.
    /// </summary>
    Unmeasurable = 0,

    /// <summary>
    /// The observer's cursor is past the end of the event sequence. The distance is exactly zero.
    /// </summary>
    CaughtUp = 1,

    /// <summary>
    /// The event sequence is ahead of the observer's cursor and nothing shows it has stopped moving.
    /// </summary>
    Lagging = 2,

    /// <summary>
    /// An event the observer observes has been waiting, its cursor has not moved for the stall window and the event sequence kept advancing.
    /// </summary>
    Stalled = 3
}

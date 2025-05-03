// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the status of an observer.
/// </summary>
public enum ObserverRunningState
{
    /// <summary>
    /// Observer is in an unknown state.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Observer is active and waiting for new events.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Observer is suspended.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Observer is replaying.
    /// </summary>
    Replaying = 3,

    /// <summary>
    /// Observer is disconnected.
    /// </summary>
    Disconnected = 4,
}

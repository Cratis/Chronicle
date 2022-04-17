// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation;

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
    /// Observer is subscribing.
    /// </summary>
    Subscribing = 1,

    /// <summary>
    /// Observer is in rewind mode for all.
    /// </summary>
    Rewinding = 2,

    /// <summary>
    /// Observer is replaying.
    /// </summary>
    Replaying = 3,

    /// <summary>
    /// Observer is catching up due to being behind current sequence number in event log.
    /// </summary>
    CatchingUp = 4,

    /// <summary>
    /// Observer is active and waiting for new events.
    /// </summary>
    Active = 5,

    /// <summary>
    /// Observer is paused.
    /// </summary>
    Paused = 6,

    /// <summary>
    /// Observer is stopped.
    /// </summary>
    Stopped = 7,

    /// <summary>
    /// Observer is suspended.
    /// </summary>
    Suspended = 8,

    /// <summary>
    /// Observer is failed.
    /// </summary>
    Failed = 9,
}
